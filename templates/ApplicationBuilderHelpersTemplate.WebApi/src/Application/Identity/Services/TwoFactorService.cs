using Application.Identity.Interfaces.Inbound;
using Application.Identity.Models;
using Domain.Identity.Exceptions;
using Domain.Identity.Interfaces;
using Domain.Shared.Exceptions;
using System.Security.Cryptography;
using System.Text;

namespace Application.Identity.Services;

internal sealed class TwoFactorService(
    IUserRepository userRepository,
    IIdentityUnitOfWork unitOfWork) : ITwoFactorService
{
    private const int RecoveryCodeCount = 10;
    private const int RecoveryCodeLength = 8;

    public async Task<TwoFactorSetupInfo> Setup2faAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByIdAsync(userId, cancellationToken).ConfigureAwait(false)
            ?? throw new EntityNotFoundException("User", userId.ToString());

        // Generate authenticator key
        var key = GenerateAuthenticatorKey();
        user.SetAuthenticatorKey(key);
        userRepository.Update(user);
        await unitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);

        // Create URI for QR code
        var issuer = "ApplicationBuilder";
        var accountName = user.UserName ?? user.Email ?? user.Id.ToString();
        var uri = $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(accountName)}?secret={key}&issuer={Uri.EscapeDataString(issuer)}";

        return new TwoFactorSetupInfo(
            SharedKey: key,
            AuthenticatorUri: uri,
            FormattedSharedKey: FormatKey(key));
    }

    public async Task<IReadOnlyCollection<string>> Enable2faAsync(Guid userId, string verificationCode, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByIdAsync(userId, cancellationToken).ConfigureAwait(false)
            ?? throw new EntityNotFoundException("User", userId.ToString());

        if (string.IsNullOrEmpty(user.AuthenticatorKey))
        {
            throw new TwoFactorException("2FA has not been set up for this user.");
        }

        // Verify the code
        if (!VerifyTotpCode(user.AuthenticatorKey, verificationCode))
        {
            throw new TwoFactorException("Invalid verification code.");
        }

        // Enable 2FA and generate recovery codes
        user.SetTwoFactorEnabled(true);
        var recoveryCodes = GenerateRecoveryCodes();
        user.SetRecoveryCodes(string.Join(";", recoveryCodes));

        userRepository.Update(user);
        await unitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);

        return recoveryCodes;
    }

    public async Task Disable2faAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByIdAsync(userId, cancellationToken).ConfigureAwait(false)
            ?? throw new EntityNotFoundException("User", userId.ToString());

        if (!user.TwoFactorEnabled)
        {
            throw new TwoFactorException("Two-factor authentication is not enabled for this user.");
        }

        user.SetTwoFactorEnabled(false);
        user.SetAuthenticatorKey(null);
        user.SetRecoveryCodes(null);

        userRepository.Update(user);
        await unitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> Verify2faCodeAsync(Guid userId, string code, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByIdAsync(userId, cancellationToken).ConfigureAwait(false)
            ?? throw new EntityNotFoundException("User", userId.ToString());

        if (!user.TwoFactorEnabled || string.IsNullOrEmpty(user.AuthenticatorKey))
        {
            return false;
        }

        // First try TOTP code
        if (VerifyTotpCode(user.AuthenticatorKey, code))
        {
            return true;
        }

        // Then try recovery code
        if (!string.IsNullOrEmpty(user.RecoveryCodes))
        {
            var recoveryCodes = user.RecoveryCodes.Split(';').ToList();
            if (recoveryCodes.Contains(code))
            {
                recoveryCodes.Remove(code);
                user.SetRecoveryCodes(recoveryCodes.Count > 0 ? string.Join(";", recoveryCodes) : null);
                userRepository.Update(user);
                await unitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);
                return true;
            }
        }

        return false;
    }

    public async Task<IReadOnlyCollection<string>> GenerateRecoveryCodesAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByIdAsync(userId, cancellationToken).ConfigureAwait(false)
            ?? throw new EntityNotFoundException("User", userId.ToString());

        if (!user.TwoFactorEnabled)
        {
            throw new TwoFactorException("Two-factor authentication is not enabled for this user.");
        }

        var recoveryCodes = GenerateRecoveryCodes();
        user.SetRecoveryCodes(string.Join(";", recoveryCodes));

        userRepository.Update(user);
        await unitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);

        return recoveryCodes;
    }

    public async Task<int> GetRecoveryCodeCountAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByIdAsync(userId, cancellationToken).ConfigureAwait(false)
            ?? throw new EntityNotFoundException("User", userId.ToString());

        if (string.IsNullOrEmpty(user.RecoveryCodes))
        {
            return 0;
        }

        return user.RecoveryCodes.Split(';', StringSplitOptions.RemoveEmptyEntries).Length;
    }

    private static string GenerateAuthenticatorKey()
    {
        var bytes = new byte[20];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Base32Encode(bytes);
    }

    private static string FormatKey(string key)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < key.Length; i++)
        {
            if (i > 0 && i % 4 == 0)
            {
                sb.Append(' ');
            }
            sb.Append(key[i]);
        }
        return sb.ToString();
    }

    private static IReadOnlyCollection<string> GenerateRecoveryCodes()
    {
        var codes = new string[RecoveryCodeCount];
        using var rng = RandomNumberGenerator.Create();

        for (int i = 0; i < RecoveryCodeCount; i++)
        {
            var bytes = new byte[RecoveryCodeLength];
            rng.GetBytes(bytes);
            codes[i] = Convert.ToHexString(bytes)[..RecoveryCodeLength].ToUpperInvariant();
        }

        return codes;
    }

    private static bool VerifyTotpCode(string secret, string code)
    {
        // Simple TOTP verification (in production, use a proper TOTP library)
        if (string.IsNullOrEmpty(code) || code.Length != 6)
        {
            return false;
        }

        try
        {
            var secretBytes = Base32Decode(secret);
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30;

            // Check current and adjacent time windows
            for (int i = -1; i <= 1; i++)
            {
                var expectedCode = GenerateTotpCode(secretBytes, timestamp + i);
                if (expectedCode == code)
                {
                    return true;
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private static string GenerateTotpCode(byte[] secret, long timestamp)
    {
        var timestampBytes = BitConverter.GetBytes(timestamp);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(timestampBytes);
        }

        using var hmac = new HMACSHA1(secret);
        var hash = hmac.ComputeHash(timestampBytes);

        var offset = hash[^1] & 0x0F;
        var binary = ((hash[offset] & 0x7F) << 24) |
                     ((hash[offset + 1] & 0xFF) << 16) |
                     ((hash[offset + 2] & 0xFF) << 8) |
                     (hash[offset + 3] & 0xFF);

        var otp = binary % 1000000;
        return otp.ToString("D6");
    }

    private static string Base32Encode(byte[] data)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var result = new StringBuilder();
        int buffer = 0, bitsLeft = 0;

        foreach (var b in data)
        {
            buffer = (buffer << 8) | b;
            bitsLeft += 8;
            while (bitsLeft >= 5)
            {
                result.Append(alphabet[(buffer >> (bitsLeft - 5)) & 0x1F]);
                bitsLeft -= 5;
            }
        }

        if (bitsLeft > 0)
        {
            result.Append(alphabet[(buffer << (5 - bitsLeft)) & 0x1F]);
        }

        return result.ToString();
    }

    private static byte[] Base32Decode(string input)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        input = input.TrimEnd('=').ToUpperInvariant();

        var output = new List<byte>();
        int buffer = 0, bitsLeft = 0;

        foreach (var c in input)
        {
            var index = alphabet.IndexOf(c);
            if (index < 0) continue;

            buffer = (buffer << 5) | index;
            bitsLeft += 5;

            if (bitsLeft >= 8)
            {
                output.Add((byte)(buffer >> (bitsLeft - 8)));
                bitsLeft -= 8;
            }
        }

        return [.. output];
    }
}

using Application.Identity.Interfaces.Inbound;
using Domain.Identity.Interfaces;

namespace Application.Identity.Services;

internal sealed class AuthMethodGuardService(
    IUserRepository userRepository,
    IPasskeyRepository passkeyRepository) : IAuthMethodGuardService
{
    public async Task<bool> CanRemovePasskeyAsync(Guid userId, Guid credentialId, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByIdAsync(userId, cancellationToken);
        if (user is null) return false;

        var passkeys = await passkeyRepository.GetCredentialsByUserIdAsync(userId, cancellationToken);
        var hasPassword = !string.IsNullOrEmpty(user.PasswordHash);
        var oauthProviderCount = user.IdentityLinks.Count;

        // Can remove if: has password, or has other OAuth providers, or has more than one passkey
        return hasPassword || oauthProviderCount > 0 || passkeys.Count > 1;
    }

    public async Task<bool> CanUnlinkProviderAsync(Guid userId, string provider, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByIdAsync(userId, cancellationToken);
        if (user is null) return false;

        var passkeys = await passkeyRepository.GetCredentialsByUserIdAsync(userId, cancellationToken);
        var hasPassword = !string.IsNullOrEmpty(user.PasswordHash);
        var otherProviderCount = user.IdentityLinks
            .Count(l => !string.Equals(l.Provider.ToString(), provider, StringComparison.OrdinalIgnoreCase));

        // Can unlink if: has password, or has passkeys, or has other OAuth providers
        return hasPassword || passkeys.Count > 0 || otherProviderCount > 0;
    }

    public async Task<int> GetAuthMethodCountAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByIdAsync(userId, cancellationToken);
        if (user is null) return 0;

        var passkeys = await passkeyRepository.GetCredentialsByUserIdAsync(userId, cancellationToken);
        var hasPassword = !string.IsNullOrEmpty(user.PasswordHash);

        var count = 0;
        if (hasPassword) count++;
        count += user.IdentityLinks.Count;
        count += passkeys.Count;

        return count;
    }
}

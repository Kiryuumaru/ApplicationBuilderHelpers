using Application.Server.Identity.Interfaces.Inbound;
using Application.Server.Identity.Interfaces.Outbound;
using Domain.Identity.Exceptions;
using Domain.Identity.Entities;
using Domain.Shared.Exceptions;

namespace Application.Server.Identity.Services;

internal sealed class PasswordService(
    IUserRepository userRepository,
    IPasswordVerifier passwordVerifier,
    IPasswordHashService passwordHashService,
    IPasswordStrengthValidator passwordStrengthValidator,
    IPasswordResetTokenService passwordResetTokenService) : IPasswordService
{
    public async Task ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByIdAsync(userId, cancellationToken).ConfigureAwait(false)
            ?? throw new EntityNotFoundException("User", userId.ToString());

        if (string.IsNullOrEmpty(user.PasswordHash))
        {
            throw new ValidationException("Password", "User does not have a password set.");
        }

        if (!passwordVerifier.Verify(user.PasswordHash, currentPassword))
        {
            throw new InvalidPasswordException();
        }

        // Validate new password strength
        var passwordErrors = await passwordStrengthValidator.ValidateAsync(user, newPassword, cancellationToken).ConfigureAwait(false);
        if (passwordErrors.Count > 0)
        {
            throw new PasswordValidationException($"Invalid password: {string.Join("; ", passwordErrors)}");
        }

        user.SetPasswordHash(passwordHashService.Hash(user, newPassword));
        await userRepository.SaveAsync(user, cancellationToken).ConfigureAwait(false);
    }

    public async Task ResetPasswordAsync(Guid userId, string newPassword, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByIdAsync(userId, cancellationToken).ConfigureAwait(false)
            ?? throw new EntityNotFoundException("User", userId.ToString());

        var passwordErrors = await passwordStrengthValidator.ValidateAsync(user, newPassword, cancellationToken).ConfigureAwait(false);
        if (passwordErrors.Count > 0)
        {
            throw new PasswordValidationException($"Invalid password: {string.Join("; ", passwordErrors)}");
        }

        user.SetPasswordHash(passwordHashService.Hash(user, newPassword));
        await userRepository.SaveAsync(user, cancellationToken).ConfigureAwait(false);
    }

    public async Task LinkPasswordAsync(Guid userId, string username, string password, string? email, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByIdAsync(userId, cancellationToken).ConfigureAwait(false)
            ?? throw new EntityNotFoundException("User", userId.ToString());

        // SECURITY: Prevent overwriting existing password
        if (!string.IsNullOrEmpty(user.PasswordHash))
        {
            throw new ValidationException("Password", "User already has a password linked. Use change password instead.");
        }

        // Check if username is already taken
        var existingUser = await userRepository.FindByUsernameAsync(username, cancellationToken).ConfigureAwait(false);
        if (existingUser is not null && existingUser.Id != userId)
        {
            throw new DuplicateEntityException("Username", username);
        }

        // Check if email is already taken
        if (!string.IsNullOrEmpty(email))
        {
            var existingByEmail = await userRepository.FindByEmailAsync(email, cancellationToken).ConfigureAwait(false);
            if (existingByEmail is not null && existingByEmail.Id != userId)
            {
                throw new DuplicateEntityException("Email", email);
            }
        }

        var passwordErrors = await passwordStrengthValidator.ValidateAsync(user, password, cancellationToken).ConfigureAwait(false);
        if (passwordErrors.Count > 0)
        {
            throw new PasswordValidationException($"Invalid password: {string.Join("; ", passwordErrors)}");
        }

        // Upgrade from anonymous if needed
        if (user.IsAnonymous)
        {
            user.UpgradeFromAnonymous(username);
        }
        else
        {
            user.SetUserName(username);
            user.SetNormalizedUserName(username.ToUpperInvariant());
        }

        if (!string.IsNullOrEmpty(email))
        {
            user.SetEmail(email);
        }

        user.SetPasswordHash(passwordHashService.Hash(user, password));
        await userRepository.SaveAsync(user, cancellationToken).ConfigureAwait(false);
    }

    public async Task<string?> GeneratePasswordResetTokenAsync(string email, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByEmailAsync(email, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return null;
        }

        return await passwordResetTokenService.GenerateResetTokenAsync(user, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> ResetPasswordWithTokenAsync(string email, string token, string newPassword, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByEmailAsync(email, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return false;
        }

        return await passwordResetTokenService.ResetPasswordWithTokenAsync(user, token, newPassword, cancellationToken).ConfigureAwait(false);
    }
}

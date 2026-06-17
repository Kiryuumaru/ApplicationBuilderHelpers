using Application.Identity.Interfaces.Outbound;
using Domain.Identity.Models;
using Domain.Identity.Entities;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Identity.Services;

internal sealed class AspNetIdentityPasswordResetTokenService(UserManager<User> userManager) : IPasswordResetTokenService
{
    public Task<string> GenerateResetTokenAsync(User user, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);

        cancellationToken.ThrowIfCancellationRequested();
        return userManager.GeneratePasswordResetTokenAsync(user);
    }

    public async Task<bool> ResetPasswordWithTokenAsync(User user, string token, string newPassword, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("Token cannot be null or whitespace.", nameof(token));
        }

        if (string.IsNullOrWhiteSpace(newPassword))
        {
            throw new ArgumentException("New password cannot be null or whitespace.", nameof(newPassword));
        }

        cancellationToken.ThrowIfCancellationRequested();

        var result = await userManager.ResetPasswordAsync(user, token, newPassword).ConfigureAwait(false);
        return result.Succeeded;
    }
}

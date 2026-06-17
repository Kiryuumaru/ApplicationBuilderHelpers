namespace Application.Server.Identity.Interfaces.Inbound;

/// <summary>
/// Service for password management operations.
/// Split from IIdentityService to follow single responsibility principle.
/// </summary>
public interface IPasswordService
{
    /// <summary>
    /// Changes a user's password.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="currentPassword">The current password for verification.</param>
    /// <param name="newPassword">The new password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken);

    /// <summary>
    /// Resets a user's password (admin operation, no verification required).
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="newPassword">The new password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ResetPasswordAsync(Guid userId, string newPassword, CancellationToken cancellationToken);

    /// <summary>
    /// Links a password to a user's account, upgrading anonymous users to full accounts.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="username">The username to set.</param>
    /// <param name="password">The password to set.</param>
    /// <param name="email">Optional email to set.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LinkPasswordAsync(Guid userId, string username, string password, string? email, CancellationToken cancellationToken);

    /// <summary>
    /// Generates a password reset token for the user.
    /// </summary>
    /// <param name="email">The user's email address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The reset token, or null if user not found.</returns>
    Task<string?> GeneratePasswordResetTokenAsync(string email, CancellationToken cancellationToken);

    /// <summary>
    /// Resets a user's password using a reset token.
    /// </summary>
    /// <param name="email">The user's email address.</param>
    /// <param name="token">The reset token.</param>
    /// <param name="newPassword">The new password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if reset succeeded.</returns>
    Task<bool> ResetPasswordWithTokenAsync(string email, string token, string newPassword, CancellationToken cancellationToken);
}

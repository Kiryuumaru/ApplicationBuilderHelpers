using Application.Client.Identity.Models;

namespace Application.Client.Identity.Interfaces.Inbound;

/// <summary>
/// Interface for user profile operations.
/// </summary>
public interface IUserProfileClient
{
    /// <summary>
    /// Gets the current user's profile from the /me endpoint.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user's profile information.</returns>
    Task<UserProfile?> GetCurrentUserAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes the current user's password.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="currentPassword">The current password.</param>
    /// <param name="newPassword">The new password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if successful, false otherwise.</returns>
    Task<(bool Success, string? ErrorMessage)> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes the current user's username.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="newUsername">The new username.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated user profile on success, or an error message on failure.</returns>
    Task<(UserProfile? Profile, string? ErrorMessage)> ChangeUsernameAsync(Guid userId, string newUsername, CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes the current user's email address.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="newEmail">The new email address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated user profile on success, or an error message on failure.</returns>
    Task<(UserProfile? Profile, string? ErrorMessage)> ChangeEmailAsync(Guid userId, string newEmail, CancellationToken cancellationToken = default);
}

using Application.Client.Identity.Models;

namespace Application.Client.Identity.Interfaces.Inbound;

/// <summary>
/// Interface for IAM user operations.
/// </summary>
public interface IUsersClient
{
    /// <summary>
    /// Lists all users.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of users.</returns>
    Task<List<IamUser>> ListUsersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by ID.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user, or null if not found.</returns>
    Task<IamUser?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a user's profile.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="request">The update request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated user.</returns>
    Task<IamUser?> UpdateUserAsync(Guid userId, UpdateUserRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if successful.</returns>
    Task<bool> DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the effective permissions for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user's effective permissions.</returns>
    Task<UserPermissions?> GetUserPermissionsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets a user's password (admin operation).
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="newPassword">The new password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if successful.</returns>
    Task<bool> ResetUserPasswordAsync(Guid userId, string newPassword, CancellationToken cancellationToken = default);
}

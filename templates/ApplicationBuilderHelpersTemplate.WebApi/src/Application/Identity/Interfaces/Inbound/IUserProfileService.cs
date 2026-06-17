using Application.Identity.Models;

namespace Application.Identity.Interfaces.Inbound;

/// <summary>
/// Service for user profile management operations.
/// Split from IIdentityService to follow single responsibility principle.
/// </summary>
public interface IUserProfileService
{
    /// <summary>
    /// Gets a user by their ID.
    /// </summary>
    /// <param name="id">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user DTO, or null if not found.</returns>
    Task<UserDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a user by their username.
    /// </summary>
    /// <param name="username">The username.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user DTO, or null if not found.</returns>
    Task<UserDto?> GetByUsernameAsync(string username, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a user by their email address.
    /// </summary>
    /// <param name="email">The email address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user DTO, or null if not found.</returns>
    Task<UserDto?> GetByEmailAsync(string email, CancellationToken cancellationToken);

    /// <summary>
    /// Lists all users.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of user DTOs.</returns>
    Task<IReadOnlyCollection<UserDto>> ListAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Updates a user's profile.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="request">The update request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateUserAsync(Guid userId, UserUpdateRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Changes the user's username.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="newUsername">The new username.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ChangeUsernameAsync(Guid userId, string newUsername, CancellationToken cancellationToken);

    /// <summary>
    /// Changes the user's email address.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="newEmail">The new email address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ChangeEmailAsync(Guid userId, string newEmail, CancellationToken cancellationToken);

    /// <summary>
    /// Links an email to a user's account.
    /// Email alone does not upgrade anonymous users - they need a password, OAuth, or passkey.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="email">The email to link.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LinkEmailAsync(Guid userId, string email, CancellationToken cancellationToken);

    /// <summary>
    /// Unlinks the email from the user's account.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UnlinkEmailAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Unlinks an external login provider from the user's account.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="provider">The external login provider to unlink.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UnlinkExternalLoginAsync(Guid userId, Domain.Identity.Enums.ExternalLoginProvider provider, CancellationToken cancellationToken);
}

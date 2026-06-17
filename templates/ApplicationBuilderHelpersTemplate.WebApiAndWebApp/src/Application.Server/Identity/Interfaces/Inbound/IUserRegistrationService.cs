using Application.Server.Identity.Models;

namespace Application.Server.Identity.Interfaces.Inbound;

/// <summary>
/// Service for user registration operations.
/// Split from IIdentityService to follow single responsibility principle.
/// </summary>
public interface IUserRegistrationService
{
    /// <summary>
    /// Registers a new user.
    /// If request is null, creates an anonymous (guest) user with no credentials.
    /// Anonymous users can later link an identity to upgrade to a full account.
    /// </summary>
    /// <param name="request">Registration request, or null for anonymous user.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created user DTO.</returns>
    Task<UserDto> RegisterUserAsync(UserRegistrationRequest? request, CancellationToken cancellationToken);

    /// <summary>
    /// Registers a user from an external identity provider (OAuth).
    /// </summary>
    /// <param name="request">External registration request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created user DTO.</returns>
    Task<UserDto> RegisterExternalAsync(ExternalUserRegistrationRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Upgrades an anonymous user to a full account after linking a passkey.
    /// Passkeys are passwordless so no username is required.
    /// </summary>
    /// <param name="userId">The user ID to upgrade.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpgradeAnonymousWithPasskeyAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a user account.
    /// </summary>
    /// <param name="userId">The user ID to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteUserAsync(Guid userId, CancellationToken cancellationToken);
}

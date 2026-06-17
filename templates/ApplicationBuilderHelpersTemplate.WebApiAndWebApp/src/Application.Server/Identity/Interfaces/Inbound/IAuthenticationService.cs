using Application.Server.Identity.Models;

namespace Application.Server.Identity.Interfaces.Inbound;

/// <summary>
/// Service for user authentication operations.
/// Split from IIdentityService to follow single responsibility principle.
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Validates user credentials without creating a session.
    /// Use this when session creation is handled separately (e.g., by controller).
    /// </summary>
    /// <param name="username">The username.</param>
    /// <param name="password">The password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Validation result with user info if successful.</returns>
    Task<CredentialValidationResult> ValidateCredentialsAsync(string username, string password, CancellationToken cancellationToken);

    /// <summary>
    /// Gets user info by ID for session creation (used by external auth flows).
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Validation result with user info.</returns>
    Task<CredentialValidationResult> GetUserForSessionAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Authenticates a user with username and password.
    /// </summary>
    /// <param name="username">The username.</param>
    /// <param name="password">The password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user session DTO.</returns>
    Task<UserSessionDto> AuthenticateAsync(string username, string password, CancellationToken cancellationToken);

    /// <summary>
    /// Authenticates a user with username and password, returning detailed result.
    /// This method handles 2FA requirements.
    /// </summary>
    /// <param name="username">The username.</param>
    /// <param name="password">The password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Authentication result indicating success, 2FA requirement, or failure.</returns>
    Task<AuthenticationResultDto> AuthenticateWithResultAsync(string username, string password, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a session for an externally authenticated user (e.g., OAuth).
    /// No password verification is performed - caller must have already verified external identity.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user session DTO.</returns>
    Task<UserSessionDto> CreateSessionForUserAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Completes authentication after 2FA verification.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="code">The 2FA code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user session DTO.</returns>
    Task<UserSessionDto> Complete2faAuthenticationAsync(Guid userId, string code, CancellationToken cancellationToken);
}

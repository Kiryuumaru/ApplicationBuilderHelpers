using Application.Identity.Models;

namespace Application.Identity.Interfaces.Inbound;

/// <summary>
/// Service for user authentication token generation and rotation.
/// Coordinates token generation with session management atomically.
/// </summary>
public interface IUserTokenService
{
    /// <summary>
    /// Creates a new login session and generates access/refresh tokens atomically.
    /// Fetches user information (username) internally from the userId.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="deviceInfo">Optional device information from HTTP context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Token pair and session ID.</returns>
    Task<UserTokenResult> CreateSessionWithTokensAsync(
        Guid userId,
        SessionDeviceInfo? deviceInfo,
        CancellationToken cancellationToken);

    /// <summary>
    /// Rotates tokens for an existing session atomically.
    /// Generates new access/refresh tokens and updates the session with the new refresh token hash.
    /// </summary>
    /// <param name="sessionId">The existing session ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>New token pair and session ID.</returns>
    Task<UserTokenResult> RotateTokensAsync(
        Guid sessionId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Validates a refresh token and rotates to new tokens if valid.
    /// Performs all validation: token parsing, user extraction, permission check, session validation.
    /// </summary>
    /// <param name="refreshToken">The refresh token to validate and rotate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing new tokens or error information.</returns>
    Task<TokenRefreshResult> RefreshTokensAsync(
        string refreshToken,
        CancellationToken cancellationToken);
}

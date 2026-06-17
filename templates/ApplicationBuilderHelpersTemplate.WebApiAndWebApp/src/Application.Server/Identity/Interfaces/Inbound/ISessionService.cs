using Application.Server.Identity.Models;

namespace Application.Server.Identity.Interfaces.Inbound;

/// <summary>
/// Service for managing user sessions.
/// Split from IIdentityService to follow single responsibility principle.
/// </summary>
public interface ISessionService
{
    /// <summary>
    /// Gets a session by its ID.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The session DTO, or null if not found.</returns>
    Task<SessionDto?> GetByIdAsync(Guid sessionId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets all active (non-revoked, non-expired) sessions for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of active sessions.</returns>
    Task<IReadOnlyCollection<SessionDto>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Revokes a specific session by its ID.
    /// </summary>
    /// <param name="sessionId">The session ID to revoke.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the session was found and revoked, false otherwise.</returns>
    Task<bool> RevokeAsync(Guid sessionId, CancellationToken cancellationToken);

    /// <summary>
    /// Revokes a session that belongs to a specific user.
    /// Returns false if the session doesn't exist or doesn't belong to the user.
    /// </summary>
    /// <param name="userId">The user ID who owns the session.</param>
    /// <param name="sessionId">The session ID to revoke.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the session was found, belonged to the user, and was revoked; false otherwise.</returns>
    Task<bool> RevokeForUserAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken);

    /// <summary>
    /// Revokes all sessions for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of sessions revoked.</returns>
    Task<int> RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Revokes all sessions for a user except the specified session.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="exceptSessionId">The session ID to keep.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of sessions revoked.</returns>
    Task<int> RevokeAllExceptAsync(Guid userId, Guid exceptSessionId, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes all expired sessions older than the specified date.
    /// Used for cleanup jobs.
    /// </summary>
    /// <param name="olderThan">Delete sessions older than this date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of sessions deleted.</returns>
    Task<int> DeleteExpiredAsync(DateTimeOffset olderThan, CancellationToken cancellationToken);

    /// <summary>
    /// Validates a session and returns its details if valid.
    /// Checks that the session exists, is not revoked, and optionally validates the refresh token hash.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="refreshTokenHash">Optional refresh token hash to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The session DTO if valid, null otherwise.</returns>
    Task<SessionDto?> ValidateSessionAsync(Guid sessionId, string? refreshTokenHash, CancellationToken cancellationToken);

    /// <summary>
    /// Validates a session using the raw refresh token (hashing is done internally).
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="refreshToken">The raw refresh token (will be hashed internally).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The session DTO if valid, null otherwise.</returns>
    Task<SessionDto?> ValidateSessionWithTokenAsync(Guid sessionId, string refreshToken, CancellationToken cancellationToken);

    /// <summary>
    /// Updates the refresh token hash for a session (token rotation).
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="newRefreshTokenHash">The new refresh token hash.</param>
    /// <param name="newExpiresAt">The new expiration time for the session.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateRefreshTokenAsync(Guid sessionId, string newRefreshTokenHash, DateTimeOffset newExpiresAt, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new login session.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="refreshTokenHash">The hashed refresh token.</param>
    /// <param name="expiresAt">When the session expires.</param>
    /// <param name="deviceName">The device name.</param>
    /// <param name="userAgent">The user agent string.</param>
    /// <param name="ipAddress">The IP address.</param>
    /// <param name="sessionId">Optional pre-generated session ID. If not provided, a new GUID is generated.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created session ID.</returns>
    Task<Guid> CreateSessionAsync(
        Guid userId,
        string refreshTokenHash,
        DateTimeOffset expiresAt,
        string? deviceName,
        string? userAgent,
        string? ipAddress,
        Guid? sessionId,
        CancellationToken cancellationToken);
}

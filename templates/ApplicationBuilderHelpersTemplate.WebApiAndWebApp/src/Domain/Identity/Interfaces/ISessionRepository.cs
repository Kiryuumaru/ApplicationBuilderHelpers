using Domain.Identity.Entities;

namespace Domain.Identity.Interfaces;

/// <summary>
/// Repository for login session persistence operations.
/// </summary>
public interface ISessionRepository
{
    /// <summary>
    /// Creates a new login session.
    /// </summary>
    /// <param name="session">The session to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CreateAsync(LoginSession session, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a session by its ID.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The session, or null if not found.</returns>
    Task<LoginSession?> GetByIdAsync(Guid sessionId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets all active (non-revoked, non-expired) sessions for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of active sessions.</returns>
    Task<IReadOnlyCollection<LoginSession>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Updates an existing session (e.g., after token rotation).
    /// </summary>
    /// <param name="session">The session to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateAsync(LoginSession session, CancellationToken cancellationToken);

    /// <summary>
    /// Revokes a specific session by its ID.
    /// </summary>
    /// <param name="sessionId">The session ID to revoke.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the session was found and revoked, false otherwise.</returns>
    Task<bool> RevokeAsync(Guid sessionId, CancellationToken cancellationToken);

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
    /// <param name="exceptSessionId">The session ID to keep active.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of sessions revoked.</returns>
    Task<int> RevokeAllExceptAsync(Guid userId, Guid exceptSessionId, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes expired and revoked sessions older than the specified age.
    /// </summary>
    /// <param name="olderThan">Delete sessions that expired or were revoked before this time.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of sessions deleted.</returns>
    Task<int> DeleteExpiredAsync(DateTimeOffset olderThan, CancellationToken cancellationToken);
}

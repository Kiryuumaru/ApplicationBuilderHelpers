using Application.Client.Identity.Models;

namespace Application.Client.Identity.Interfaces.Inbound;

/// <summary>
/// Interface for session management API operations.
/// </summary>
public interface ISessionsClient
{
    /// <summary>
    /// Gets all active sessions for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of active sessions.</returns>
    Task<List<SessionInfo>> ListSessionsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes a specific session.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="sessionId">The session ID to revoke.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if successful.</returns>
    Task<bool> RevokeSessionAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all sessions for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of revoked sessions.</returns>
    Task<int> RevokeAllSessionsAsync(Guid userId, CancellationToken cancellationToken = default);
}

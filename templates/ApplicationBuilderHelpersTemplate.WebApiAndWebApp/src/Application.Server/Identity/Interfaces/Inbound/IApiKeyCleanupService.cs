namespace Application.Server.Identity.Interfaces.Inbound;

/// <summary>
/// Service for cleaning up expired and revoked API keys.
/// </summary>
public interface IApiKeyCleanupService
{
    /// <summary>
    /// Deletes API keys that have expired or been revoked for longer than the retention period.
    /// </summary>
    /// <param name="expiredRetentionDays">Delete keys that expired more than this many days ago.</param>
    /// <param name="revokedRetentionDays">Delete revoked keys that were revoked more than this many days ago.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of keys deleted.</returns>
    Task<int> CleanupExpiredAndRevokedKeysAsync(int expiredRetentionDays, int revokedRetentionDays, CancellationToken cancellationToken);
}

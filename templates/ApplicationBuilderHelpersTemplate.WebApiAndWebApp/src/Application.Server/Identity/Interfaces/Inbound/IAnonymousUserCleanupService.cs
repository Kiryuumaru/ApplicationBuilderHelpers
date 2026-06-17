namespace Application.Server.Identity.Interfaces.Inbound;

/// <summary>
/// Service for cleaning up abandoned anonymous user accounts.
/// </summary>
public interface IAnonymousUserCleanupService
{
    /// <summary>
    /// Deletes anonymous user accounts that have been inactive for longer than the specified retention period.
    /// </summary>
    /// <param name="retentionDays">Number of days of inactivity before an anonymous account is considered abandoned.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of accounts deleted.</returns>
    Task<int> CleanupAbandonedAccountsAsync(int retentionDays, CancellationToken cancellationToken);
}

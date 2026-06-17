using Application.Server.Identity.Interfaces.Inbound;
using Application.Server.Identity.Interfaces.Outbound;
using Microsoft.Extensions.Logging;

namespace Application.Server.Identity.Services;

internal sealed class AnonymousUserCleanupService(
    IUserRepository userRepository,
    ILogger<AnonymousUserCleanupService> logger) : IAnonymousUserCleanupService
{

    public async Task<int> CleanupAbandonedAccountsAsync(int retentionDays, CancellationToken cancellationToken)
    {
        if (retentionDays < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(retentionDays), "Retention days must be at least 1.");
        }

        var cutoffDate = DateTimeOffset.UtcNow.AddDays(-retentionDays);

        logger.LogInformation(
            "Starting cleanup of abandoned anonymous accounts older than {CutoffDate} ({RetentionDays} days)",
            cutoffDate,
            retentionDays);

        var deletedCount = await userRepository.DeleteAbandonedAnonymousUsersAsync(cutoffDate, cancellationToken);

        if (deletedCount > 0)
        {
            logger.LogInformation("Deleted {Count} abandoned anonymous user accounts", deletedCount);
        }
        else
        {
            logger.LogDebug("No abandoned anonymous user accounts found to delete");
        }

        return deletedCount;
    }
}

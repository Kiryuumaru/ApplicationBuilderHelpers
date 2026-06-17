using Application.Server.Identity.Interfaces.Inbound;
using Domain.Identity.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.Server.Identity.Services;

internal sealed class ApiKeyCleanupService(
    IApiKeyRepository apiKeyRepository,
    ILogger<ApiKeyCleanupService> logger) : IApiKeyCleanupService
{
    public async Task<int> CleanupExpiredAndRevokedKeysAsync(
        int expiredRetentionDays,
        int revokedRetentionDays,
        CancellationToken cancellationToken)
    {
        if (expiredRetentionDays < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(expiredRetentionDays), "Expired retention days must be non-negative.");
        }

        if (revokedRetentionDays < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(revokedRetentionDays), "Revoked retention days must be at least 1.");
        }

        var expiredBefore = DateTimeOffset.UtcNow.AddDays(-expiredRetentionDays);
        var revokedBefore = DateTimeOffset.UtcNow.AddDays(-revokedRetentionDays);

        logger.LogInformation(
            "Starting API key cleanup. Deleting keys expired before {ExpiredBefore} and revoked before {RevokedBefore}",
            expiredBefore,
            revokedBefore);

        var deletedCount = await apiKeyRepository.DeleteExpiredOrRevokedAsync(
            expiredBefore,
            revokedBefore,
            cancellationToken);

        if (deletedCount > 0)
        {
            logger.LogInformation("Deleted {Count} expired or revoked API keys", deletedCount);
        }
        else
        {
            logger.LogDebug("No expired or revoked API keys found to delete");
        }

        return deletedCount;
    }
}

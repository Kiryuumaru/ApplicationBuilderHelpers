using Application.Identity.Constants;
using Domain.Identity.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Application.Identity.Workers;

internal sealed class ApiKeyCleanupWorker(
    IServiceProvider serviceProvider,
    ILogger<ApiKeyCleanupWorker> logger) : BackgroundService
{
    private const int ExpiredRetentionDays = 0;
    private const int RevokedRetentionDays = 30;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "API key cleanup worker started. Will clean up expired keys ({ExpiredRetention} day retention) and revoked keys ({RevokedRetention} day retention) every {Interval} hours",
            ExpiredRetentionDays,
            RevokedRetentionDays,
            CleanupWorkerDefaults.CleanupInterval.TotalHours);

        try
        {
            await Task.Delay(CleanupWorkerDefaults.InitialDelay, stoppingToken);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunCleanupAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error during API key cleanup");
            }

            try
            {
                await Task.Delay(CleanupWorkerDefaults.CleanupInterval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }

        logger.LogInformation("API key cleanup worker stopped");
    }

    private async Task RunCleanupAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var apiKeyRepository = scope.ServiceProvider.GetRequiredService<IApiKeyRepository>();

        var expiredBefore = DateTimeOffset.UtcNow.AddDays(-ExpiredRetentionDays);
        var revokedBefore = DateTimeOffset.UtcNow.AddDays(-RevokedRetentionDays);

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
    }
}

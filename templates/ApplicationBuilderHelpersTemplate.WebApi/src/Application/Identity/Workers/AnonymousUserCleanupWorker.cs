using Application.Identity.Constants;
using Domain.Identity.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Application.Identity.Workers;

internal sealed class AnonymousUserCleanupWorker(
    IServiceProvider serviceProvider,
    ILogger<AnonymousUserCleanupWorker> logger) : BackgroundService
{
    private const int RetentionDays = 30;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Anonymous user cleanup worker started. Will clean up accounts inactive for {RetentionDays} days every {Interval} hours",
            RetentionDays,
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
                logger.LogError(ex, "Error during anonymous user cleanup");
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

        logger.LogInformation("Anonymous user cleanup worker stopped");
    }

    private async Task RunCleanupAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        var cutoffDate = DateTimeOffset.UtcNow.AddDays(-RetentionDays);

        logger.LogInformation(
            "Starting cleanup of abandoned anonymous accounts older than {CutoffDate} ({RetentionDays} days)",
            cutoffDate,
            RetentionDays);

        var deletedCount = await userRepository.DeleteAbandonedAnonymousUsersAsync(cutoffDate, cancellationToken);

        if (deletedCount > 0)
        {
            logger.LogInformation("Deleted {Count} abandoned anonymous user accounts", deletedCount);
        }
        else
        {
            logger.LogDebug("No abandoned anonymous user accounts found to delete");
        }
    }
}

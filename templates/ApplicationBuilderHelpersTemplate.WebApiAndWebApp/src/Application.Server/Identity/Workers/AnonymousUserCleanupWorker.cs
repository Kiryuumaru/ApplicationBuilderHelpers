using Application.Server.Identity.Interfaces.Inbound;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Application.Server.Identity.Workers;

internal sealed class AnonymousUserCleanupWorker(
    IServiceProvider serviceProvider,
    ILogger<AnonymousUserCleanupWorker> logger) : BackgroundService
{
    private const int RetentionDays = 30;
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromHours(24);
    private static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(5);

    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    private readonly ILogger<AnonymousUserCleanupWorker> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Anonymous user cleanup worker started. Will clean up accounts inactive for {RetentionDays} days every {Interval} hours",
            RetentionDays,
            CleanupInterval.TotalHours);

        // Wait before first run to let the application fully initialize
        try
        {
            await Task.Delay(InitialDelay, stoppingToken);
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
                _logger.LogError(ex, "Error during anonymous user cleanup");
            }

            try
            {
                await Task.Delay(CleanupInterval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("Anonymous user cleanup worker stopped");
    }

    private async Task RunCleanupAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var cleanupService = scope.ServiceProvider.GetRequiredService<IAnonymousUserCleanupService>();

        await cleanupService.CleanupAbandonedAccountsAsync(RetentionDays, cancellationToken);
    }
}

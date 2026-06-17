using Application.Server.Identity.Interfaces.Inbound;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Application.Server.Identity.Workers;

internal sealed class ApiKeyCleanupWorker(
    IServiceProvider serviceProvider,
    ILogger<ApiKeyCleanupWorker> logger) : BackgroundService
{
    private const int ExpiredRetentionDays = 0;
    private const int RevokedRetentionDays = 30;
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromHours(24);
    private static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(5);

    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    private readonly ILogger<ApiKeyCleanupWorker> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "API key cleanup worker started. Will clean up expired keys ({ExpiredRetention} day retention) and revoked keys ({RevokedRetention} day retention) every {Interval} hours",
            ExpiredRetentionDays,
            RevokedRetentionDays,
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
                _logger.LogError(ex, "Error during API key cleanup");
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

        _logger.LogInformation("API key cleanup worker stopped");
    }

    private async Task RunCleanupAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var cleanupService = scope.ServiceProvider.GetRequiredService<IApiKeyCleanupService>();

        await cleanupService.CleanupExpiredAndRevokedKeysAsync(
            ExpiredRetentionDays,
            RevokedRetentionDays,
            cancellationToken);
    }
}

using Application.EmbeddedConfig.Extensions;
using Application.EmbeddedConfig.Interfaces.Inbound;
using Application.WeatherForecast.Interfaces.Inbound;
using ApplicationBuilderHelpers;
using ApplicationBuilderHelpers.Attributes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Presentation.Cli.Commands;

[Command("Main subcommand.")]
internal class MainCommand : Build.BaseCommand<HostApplicationBuilder>
{
    [CommandOption("location", Description = "The location to generate a weather forecast for.")]
    public string Location { get; set; } = "New York";

    [CommandOption('d', "days", Description = "Number of days to forecast (1-14).")]
    public int Days { get; set; } = 5;

    protected override ValueTask<HostApplicationBuilder> ApplicationBuilder(CancellationToken stoppingToken)
    {
        var builder = Host.CreateApplicationBuilder();
        return new ValueTask<HostApplicationBuilder>(builder);
    }

    protected override async ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
    {
        await base.Run(applicationHost, cancellationTokenSource);

        using var scope = applicationHost.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<MainCommand>>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var embeddedConfigService = scope.ServiceProvider.GetRequiredService<IEmbeddedConfigService>();

        var embeddedConfig = await embeddedConfigService.GetConfig(cancellationTokenSource.Token);

        logger.LogInformation("Embedded Config (encrypted at build time, decrypted at runtime):");
        logger.LogInformation("  Shared - Weather API URL: {ApiUrl}", embeddedConfig.SharedConfig["weather_api_url"]?.ToString() ?? "N/A");
        logger.LogInformation("  Shared - Default Location: {Location}", embeddedConfig.SharedConfig["default_location"]?.ToString() ?? "N/A");
        logger.LogInformation("  Environment - Weather API Key: {ApiKey}", embeddedConfig.EnvironmentConfig["weather_api_key"]?.ToString() ?? "N/A");
        logger.LogInformation("---");

        var forecastService = scope.ServiceProvider.GetRequiredService<IWeatherForecastService>();

        logger.LogInformation("Generating {Days}-day weather forecast for {Location}...", Days, Location);
        logger.LogInformation("---");

        var forecasts = await forecastService.GenerateForecastsAsync(Location, Days, cancellationTokenSource.Token);

        logger.LogInformation("---");
        logger.LogInformation("{Days}-Day Weather Forecast for {Location}:", Days, Location);
        logger.LogInformation("");

        foreach (var forecast in forecasts)
        {
            logger.LogInformation("  {Date}: {Condition} | High: {High:F1}°C ({HighF:F1}°F) | Low: {Low:F1}°C ({LowF:F1}°F)",
                forecast.ForecastDate,
                forecast.Condition,
                forecast.HighTemperatureCelsius,
                forecast.HighTemperatureFahrenheit,
                forecast.LowTemperatureCelsius,
                forecast.LowTemperatureFahrenheit);
            logger.LogInformation("           {Summary}", forecast.Summary);
        }

        cancellationTokenSource.Cancel();
    }
}

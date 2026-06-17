using Application.WeatherForecast.Interfaces.Inbound;
using Application.WeatherForecast.Models;
using Domain.WeatherForecast.Entities;
using Domain.WeatherForecast.Enums;
using Domain.WeatherForecast.Interfaces;
using Domain.WeatherForecast.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Application.WeatherForecast.Services;

internal sealed class WeatherForecastService(
    IWeatherForecastRepository repository,
    IWeatherForecastUnitOfWork unitOfWork,
    ILogger<WeatherForecastService> logger) : IWeatherForecastService
{
    private static readonly WeatherCondition[] Conditions = Enum.GetValues<WeatherCondition>();

    private static readonly string[] Summaries =
    [
        "Clear skies with pleasant temperatures",
        "Overcast with occasional breaks in clouds",
        "Steady rain expected throughout the day",
        "Heavy snowfall with accumulation likely",
        "Strong gusts with wind advisories in effect",
        "Severe weather warning with thunderstorms"
    ];

    public async Task<IReadOnlyList<WeatherForecastResult>> GenerateForecastsAsync(
        string location,
        int days,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(location);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(days);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(days, 14);

        logger.LogInformation("Generating {Days}-day forecast for {Location}", days, location);

        var results = new List<WeatherForecastResult>(days);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var random = new Random(location.GetHashCode());

        for (int i = 0; i < days; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var forecastDate = today.AddDays(i);
            var condition = Conditions[random.Next(Conditions.Length)];
            var baseTemp = 15.0 + random.NextDouble() * 20.0 - 5.0;
            var highTemp = Temperature.Create(Math.Round(baseTemp + random.NextDouble() * 5.0, 1));
            var lowTemp = Temperature.Create(Math.Round(baseTemp - random.NextDouble() * 8.0, 1));
            var summary = Summaries[(int)condition];

            var entity = WeatherForecastEntity.Create(
                location,
                forecastDate,
                highTemp,
                lowTemp,
                condition,
                summary);

            repository.Add(entity);

            results.Add(new WeatherForecastResult(
                entity.Id,
                entity.Location,
                entity.ForecastDate,
                entity.HighTemperature.Celsius,
                entity.LowTemperature.Celsius,
                entity.HighTemperature.Fahrenheit,
                entity.LowTemperature.Fahrenheit,
                entity.Condition,
                entity.Summary,
                DateTimeOffset.UtcNow));
        }

        await unitOfWork.CommitAsync(cancellationToken);

        logger.LogInformation("Generated {Count} forecasts for {Location}", results.Count, location);

        return results;
    }
}

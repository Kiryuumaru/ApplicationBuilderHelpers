using Domain.WeatherForecast.Enums;

namespace Application.WeatherForecast.Models;

/// <summary>
/// Result of a weather forecast creation operation.
/// </summary>
public sealed record WeatherForecastResult(
    Guid EntityId,
    string Location,
    DateOnly ForecastDate,
    double HighTemperatureCelsius,
    double LowTemperatureCelsius,
    double HighTemperatureFahrenheit,
    double LowTemperatureFahrenheit,
    WeatherCondition Condition,
    string Summary,
    DateTimeOffset CreatedAt);

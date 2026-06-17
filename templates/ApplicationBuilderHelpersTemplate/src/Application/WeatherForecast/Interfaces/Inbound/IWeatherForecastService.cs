using Application.WeatherForecast.Models;

namespace Application.WeatherForecast.Interfaces.Inbound;

/// <summary>
/// Application service for creating and retrieving weather forecasts.
/// </summary>
public interface IWeatherForecastService
{
    Task<IReadOnlyList<WeatherForecastResult>> GenerateForecastsAsync(string location, int days, CancellationToken cancellationToken = default);
}

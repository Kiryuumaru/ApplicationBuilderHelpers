using Domain.WeatherForecast.Entities;

namespace Domain.WeatherForecast.Interfaces;

/// <summary>
/// Repository contract for weather forecast persistence.
/// </summary>
public interface IWeatherForecastRepository
{
    void Add(WeatherForecastEntity entity);

    Task<WeatherForecastEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WeatherForecastEntity>> GetByLocationAsync(string location, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WeatherForecastEntity>> GetAllAsync(CancellationToken cancellationToken = default);
}

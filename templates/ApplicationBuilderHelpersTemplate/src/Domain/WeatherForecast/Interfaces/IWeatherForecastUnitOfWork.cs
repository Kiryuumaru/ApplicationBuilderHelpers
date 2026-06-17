using Domain.Shared.Interfaces;

namespace Domain.WeatherForecast.Interfaces;

/// <summary>
/// Unit of work defining the atomicity boundary for weather forecast operations.
/// </summary>
public interface IWeatherForecastUnitOfWork : IUnitOfWork;

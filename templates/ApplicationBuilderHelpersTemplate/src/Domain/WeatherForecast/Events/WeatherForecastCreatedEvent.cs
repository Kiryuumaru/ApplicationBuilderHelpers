using Domain.Shared.Models;

namespace Domain.WeatherForecast.Events;

/// <summary>
/// Domain event raised when a weather forecast is created.
/// </summary>
public sealed record WeatherForecastCreatedEvent(Guid EntityId, string Location, DateOnly ForecastDate) : DomainEvent;

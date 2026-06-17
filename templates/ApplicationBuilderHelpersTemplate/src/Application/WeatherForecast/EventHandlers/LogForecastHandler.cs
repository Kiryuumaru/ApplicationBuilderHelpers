using Application.Shared.Models;
using Domain.WeatherForecast.Events;
using Microsoft.Extensions.Logging;

namespace Application.WeatherForecast.EventHandlers;

internal sealed class LogForecastHandler(ILogger<LogForecastHandler> logger) : DomainEventHandler<WeatherForecastCreatedEvent>
{
    public override ValueTask HandleAsync(WeatherForecastCreatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("[Event] Forecast created: {Location} on {Date} (EntityId: {EntityId})",
            domainEvent.Location,
            domainEvent.ForecastDate,
            domainEvent.EntityId);

        return ValueTask.CompletedTask;
    }
}

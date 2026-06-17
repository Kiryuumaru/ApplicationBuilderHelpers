using Application.Shared.Models;
using Domain.WeatherForecast.Events;
using Microsoft.Extensions.Logging;

namespace Application.WeatherForecast.EventHandlers;

internal sealed class NotifySubscribersHandler(ILogger<NotifySubscribersHandler> logger) : DomainEventHandler<WeatherForecastCreatedEvent>
{
    public override ValueTask HandleAsync(WeatherForecastCreatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("[Event] Notifying subscribers of forecast for {Location} on {Date}",
            domainEvent.Location,
            domainEvent.ForecastDate);

        return ValueTask.CompletedTask;
    }
}

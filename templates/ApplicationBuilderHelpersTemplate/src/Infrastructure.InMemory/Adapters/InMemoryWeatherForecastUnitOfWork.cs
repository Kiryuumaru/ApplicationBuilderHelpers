using Application.Shared.Interfaces;
using Domain.Shared.Interfaces;
using Domain.WeatherForecast.Interfaces;
using Infrastructure.InMemory.Interfaces;

namespace Infrastructure.InMemory.Adapters;

internal sealed class InMemoryWeatherForecastUnitOfWork(
    IDomainEventDispatcher eventDispatcher,
    IEnumerable<ITrackableRepository> trackableRepositories) : IWeatherForecastUnitOfWork
{
    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        var trackedAggregates = trackableRepositories
            .SelectMany(r => r.GetTrackedAggregates())
            .ToList();

        var domainEvents = trackedAggregates
            .SelectMany(a => a.DomainEvents)
            .ToList();

        foreach (var aggregate in trackedAggregates)
        {
            aggregate.ClearDomainEvents();
        }

        foreach (var repository in trackableRepositories)
        {
            repository.ClearTrackedAggregates();
        }

        await eventDispatcher.DispatchAsync(domainEvents, cancellationToken);
    }
}

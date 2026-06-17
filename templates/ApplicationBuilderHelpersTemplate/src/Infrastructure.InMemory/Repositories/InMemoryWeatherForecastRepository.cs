using System.Collections.Concurrent;
using Domain.Shared.Interfaces;
using Domain.WeatherForecast.Entities;
using Domain.WeatherForecast.Interfaces;
using Infrastructure.InMemory.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.InMemory.Repositories;

internal sealed class InMemoryWeatherForecastRepository(ILogger<InMemoryWeatherForecastRepository> logger) : IWeatherForecastRepository, ITrackableRepository
{
    private static readonly ConcurrentDictionary<Guid, WeatherForecastEntity> _storage = new();

    private readonly List<IAggregateRoot> _trackedAggregates = [];

    public void Add(WeatherForecastEntity entity)
    {
        _storage[entity.Id] = entity;
        _trackedAggregates.Add(entity);

        logger.LogInformation("[InMemoryRepository] Forecast stored: {Location} on {Date}",
            entity.Location,
            entity.ForecastDate);
    }

    public Task<WeatherForecastEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _storage.TryGetValue(id, out var entity);
        return Task.FromResult(entity);
    }

    public Task<IReadOnlyList<WeatherForecastEntity>> GetByLocationAsync(string location, CancellationToken cancellationToken = default)
    {
        var results = _storage.Values
            .Where(e => string.Equals(e.Location, location, StringComparison.OrdinalIgnoreCase))
            .ToList();
        return Task.FromResult<IReadOnlyList<WeatherForecastEntity>>(results);
    }

    public Task<IReadOnlyList<WeatherForecastEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<WeatherForecastEntity>>(_storage.Values.ToList());
    }

    public IEnumerable<IAggregateRoot> GetTrackedAggregates() => _trackedAggregates;

    public void ClearTrackedAggregates() => _trackedAggregates.Clear();
}

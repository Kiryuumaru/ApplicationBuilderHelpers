using Domain.Shared.Interfaces;

namespace Infrastructure.InMemory.Interfaces;

internal interface ITrackableRepository
{
    IEnumerable<IAggregateRoot> GetTrackedAggregates();

    void ClearTrackedAggregates();
}

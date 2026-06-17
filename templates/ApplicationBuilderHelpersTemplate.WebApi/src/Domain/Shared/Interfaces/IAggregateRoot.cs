namespace Domain.Shared.Interfaces;

/// <summary>
/// Marker interface for aggregate roots that can raise domain events.
/// </summary>
public interface IAggregateRoot
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    void ClearDomainEvents();
}

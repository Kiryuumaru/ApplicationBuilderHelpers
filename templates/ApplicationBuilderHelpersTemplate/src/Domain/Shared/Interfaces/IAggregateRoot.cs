namespace Domain.Shared.Interfaces;

/// <summary>
/// Marks entities that serve as aggregate roots and can raise domain events.
/// </summary>
public interface IAggregateRoot
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    void ClearDomainEvents();
}

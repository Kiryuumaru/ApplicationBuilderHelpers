using Domain.Shared.Interfaces;

namespace Domain.Shared.Models;

/// <summary>
/// Entity that can raise domain events and serves as aggregate boundary.
/// </summary>
public abstract class AggregateRoot : AuditableEntity, IAggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected AggregateRoot(Guid id) : base(id)
    {
    }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void RemoveDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Remove(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}

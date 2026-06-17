using Domain.Shared.Interfaces;

namespace Domain.Shared.Models;

/// <summary>
/// Base record for domain events representing state changes.
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public DateTimeOffset OccurredOn { get; private set; } = DateTimeOffset.UtcNow;
}

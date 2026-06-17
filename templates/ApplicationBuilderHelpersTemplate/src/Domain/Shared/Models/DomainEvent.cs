using Domain.Shared.Interfaces;

namespace Domain.Shared.Models;

public abstract record DomainEvent : IDomainEvent
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public DateTimeOffset OccurredOn { get; private set; } = DateTimeOffset.UtcNow;
}

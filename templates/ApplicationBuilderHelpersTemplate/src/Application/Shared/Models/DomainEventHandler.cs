using Application.Shared.Interfaces;
using Domain.Shared.Interfaces;

namespace Application.Shared.Models;

public abstract class DomainEventHandler<TEvent> : IDomainEventHandler<TEvent> where TEvent : IDomainEvent
{
    public bool CanHandle(IDomainEvent domainEvent) => domainEvent is TEvent;

    public ValueTask HandleAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        if (domainEvent is TEvent typedEvent)
        {
            return HandleAsync(typedEvent, cancellationToken);
        }
        return ValueTask.CompletedTask;
    }

    public abstract ValueTask HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}

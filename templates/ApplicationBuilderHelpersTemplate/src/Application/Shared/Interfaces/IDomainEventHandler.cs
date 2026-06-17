using Domain.Shared.Interfaces;

namespace Application.Shared.Interfaces;

/// <summary>
/// Defines a handler for domain events.
/// </summary>
public interface IDomainEventHandler
{
    /// <summary>
    /// Determines whether this handler can handle the specified domain event.
    /// </summary>
    /// <param name="domainEvent">The domain event to check.</param>
    /// <returns><c>true</c> if this handler can handle the event; otherwise, <c>false</c>.</returns>
    bool CanHandle(IDomainEvent domainEvent);

    /// <summary>
    /// Handles the specified domain event.
    /// </summary>
    /// <param name="domainEvent">The domain event to handle.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    ValueTask HandleAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines a strongly-typed handler for domain events of type <typeparamref name="TEvent"/>.
/// </summary>
/// <typeparam name="TEvent">The type of domain event this handler can process.</typeparam>
public interface IDomainEventHandler<in TEvent> : IDomainEventHandler where TEvent : IDomainEvent
{
    /// <summary>
    /// Handles the specified domain event.
    /// </summary>
    /// <param name="domainEvent">The domain event to handle.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    ValueTask HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}

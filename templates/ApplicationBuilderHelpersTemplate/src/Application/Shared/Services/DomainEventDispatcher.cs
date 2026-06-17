using Application.Shared.Interfaces;
using Domain.Shared.Interfaces;

namespace Application.Shared.Services;

internal sealed class DomainEventDispatcher(IEnumerable<IDomainEventHandler> handlers) : IDomainEventDispatcher
{
    public async ValueTask DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        // Materialize to array to start all handlers immediately (hot tasks)
        var tasks = handlers
            .Where(h => h.CanHandle(domainEvent))
            .Select(h => h.HandleAsync(domainEvent, cancellationToken))
            .ToArray();

        // Await in loop - tasks are already running (hot), so we wait for all to complete
        // This avoids the allocation overhead of .AsTask() for synchronously-completed ValueTasks
        List<Exception>? exceptions = null;

        foreach (var task in tasks)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                exceptions ??= [];
                exceptions.Add(ex);
            }
        }

        if (exceptions is not null)
        {
            throw new AggregateException(exceptions);
        }
    }

    public async ValueTask DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            await DispatchAsync(domainEvent, cancellationToken);
        }
    }
}

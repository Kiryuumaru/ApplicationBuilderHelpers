namespace Domain.Shared.Interfaces;

/// <summary>
/// Base unit of work contract for coordinating persistence and domain event dispatch.
/// Feature-specific UnitOfWork interfaces inherit from this.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Commits all pending changes and dispatches domain events.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CommitAsync(CancellationToken cancellationToken = default);
}

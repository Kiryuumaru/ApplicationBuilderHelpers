namespace Application.LocalStore.Interfaces.Inbound;

/// <summary>
/// Provides thread-safe access to a local key-value store for a specific group.
/// </summary>
public interface IConcurrentLocalStore : IDisposable
{
    /// <summary>
    /// Gets the group name for this store instance.
    /// </summary>
    string Group { get; }

    /// <summary>
    /// Checks if a value with the specified ID exists in the store.
    /// </summary>
    Task<bool> Contains(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a value with the specified ID exists, throws if not found.
    /// </summary>
    Task ContainsOrError(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the value associated with the specified ID.
    /// </summary>
    Task<string?> Get(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all IDs in the current group.
    /// </summary>
    Task<string[]> GetIds(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a value for the specified ID.
    /// </summary>
    Task Set(string id, string? value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the value associated with the specified ID.
    /// </summary>
    Task Delete(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits any pending changes to the store.
    /// </summary>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back any pending changes.
    /// </summary>
    Task RollbackAsync(CancellationToken cancellationToken = default);
}

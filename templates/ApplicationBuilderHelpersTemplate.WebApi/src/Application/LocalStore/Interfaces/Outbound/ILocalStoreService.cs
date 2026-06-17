namespace Application.LocalStore.Interfaces.Outbound;

/// <summary>
/// Service for local key-value storage operations.
/// Implemented by Infrastructure layer.
/// </summary>
public interface ILocalStoreService : IDisposable
{
    Task<string?> GetAsync(string group, string id, CancellationToken cancellationToken);

    Task<string[]> GetIdsAsync(string group, CancellationToken cancellationToken);

    Task SetAsync(string group, string id, string? data, CancellationToken cancellationToken);

    Task<bool> ContainsAsync(string group, string id, CancellationToken cancellationToken);

    Task OpenAsync(CancellationToken cancellationToken);

    Task CommitAsync(CancellationToken cancellationToken);

    Task RollbackAsync(CancellationToken cancellationToken);
}

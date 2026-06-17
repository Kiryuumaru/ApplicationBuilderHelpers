namespace Application.LocalStore.Interfaces.Inbound;

/// <summary>
/// Provides concurrent access to a local key-value store with transaction support.
/// </summary>
public interface IConcurrentLocalStore : IDisposable
{
    string Group { get; }
    Task<bool> ContainsAsync(string id, CancellationToken cancellationToken = default);
    Task ContainsOrErrorAsync(string id, CancellationToken cancellationToken = default);
    Task<string?> GetAsync(string id, CancellationToken cancellationToken = default);
    Task<string[]> GetIdsAsync(CancellationToken cancellationToken = default);
    Task SetAsync(string id, string? value, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}

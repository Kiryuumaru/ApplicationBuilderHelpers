using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.LocalStore.Interfaces.Outbound;

public interface ILocalStoreService : IDisposable
{
    Task<string?> Get(string group, string id, CancellationToken cancellationToken);

    Task<string[]> GetIds(string group, CancellationToken cancellationToken);

    Task Set(string group, string id, string? data, CancellationToken cancellationToken);

    Task<bool> Contains(string group, string id, CancellationToken cancellationToken);

    Task Open(CancellationToken cancellationToken);

    Task CommitAsync(CancellationToken cancellationToken);

    Task RollbackAsync(CancellationToken cancellationToken);
}

using Application.LocalStore.Utilities;
using Application.LocalStore.Interfaces.Inbound;
using Application.LocalStore.Interfaces.Outbound;
using DisposableHelpers.Attributes;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Application.LocalStore.Services;

[Disposable]
internal partial class ConcurrentLocalStore(ILocalStoreService localStoreService, string group) : IConcurrentLocalStore
{
    private readonly ILocalStoreService localStoreService = localStoreService ?? throw new ArgumentNullException(nameof(localStoreService));
    private readonly SemaphoreSlim gate = new(1, 1);

    public string Group { get; } = LocalStoreKey.NormalizeGroup(group);

    public Task<bool> Contains(string id, CancellationToken cancellationToken = default)
    {
        var normalizedId = LocalStoreKey.NormalizeId(id);
        return WithGateAsync(() => localStoreService.Contains(Group, normalizedId, cancellationToken), cancellationToken);
    }

    public async Task ContainsOrError(string id, CancellationToken cancellationToken = default)
    {
        if (!await Contains(id, cancellationToken).ConfigureAwait(false))
        {
            throw new KeyNotFoundException($"The item with ID '{id}' does not exist in the group '{Group}'.");
        }
    }

    public Task<string?> Get(string id, CancellationToken cancellationToken = default)
    {
        var normalizedId = LocalStoreKey.NormalizeId(id);
        return WithGateAsync(() => localStoreService.Get(Group, normalizedId, cancellationToken), cancellationToken);
    }

    public Task<string[]> GetIds(CancellationToken cancellationToken = default)
    {
        return WithGateAsync(() => localStoreService.GetIds(Group, cancellationToken), cancellationToken);
    }

    public Task Set(string id, string? value, CancellationToken cancellationToken = default)
    {
        var normalizedId = LocalStoreKey.NormalizeId(id);
        return WithGateAsync(() => localStoreService.Set(Group, normalizedId, value, cancellationToken), cancellationToken);
    }

    public Task Delete(string id, CancellationToken cancellationToken = default)
    {
        return Set(id, null, cancellationToken);
    }

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        return WithGateAsync(() => localStoreService.CommitAsync(cancellationToken), cancellationToken);
    }

    public Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        return WithGateAsync(() => localStoreService.RollbackAsync(cancellationToken), cancellationToken);
    }

    private async Task<T> WithGateAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken)
    {
        VerifyNotDisposed();
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await action().ConfigureAwait(false);
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task WithGateAsync(Func<Task> action, CancellationToken cancellationToken)
    {
        await WithGateAsync(async () =>
        {
            await action().ConfigureAwait(false);
            return true;
        }, cancellationToken).ConfigureAwait(false);
    }

    protected void Dispose(bool disposing)
    {
        if (disposing)
        {
            gate.Dispose();
            localStoreService?.Dispose();
        }
    }
}

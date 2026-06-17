using Application.LocalStore.Utilities;
using Application.LocalStore.Interfaces.Inbound;
using Application.LocalStore.Interfaces.Outbound;
using DisposableHelpers.Attributes;

namespace Application.LocalStore.Services;

[Disposable]
internal partial class ConcurrentLocalStore(ILocalStoreService localStoreService, string group) : IConcurrentLocalStore
{
    private readonly SemaphoreSlim gate = new(1, 1);

    public string Group { get; } = LocalStoreKey.NormalizeGroup(group);

    public Task<bool> ContainsAsync(string id, CancellationToken cancellationToken = default)
    {
        var normalizedId = LocalStoreKey.NormalizeId(id);
        return WithGateAsync(() => localStoreService.ContainsAsync(Group, normalizedId, cancellationToken), cancellationToken);
    }

    public async Task ContainsOrErrorAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!await ContainsAsync(id, cancellationToken).ConfigureAwait(false))
        {
            throw new KeyNotFoundException($"The item with ID '{id}' does not exist in the group '{Group}'.");
        }
    }

    public Task<string?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        var normalizedId = LocalStoreKey.NormalizeId(id);
        return WithGateAsync(() => localStoreService.GetAsync(Group, normalizedId, cancellationToken), cancellationToken);
    }

    public Task<string[]> GetIdsAsync(CancellationToken cancellationToken = default)
    {
        return WithGateAsync(() => localStoreService.GetIdsAsync(Group, cancellationToken), cancellationToken);
    }

    public Task SetAsync(string id, string? value, CancellationToken cancellationToken = default)
    {
        var normalizedId = LocalStoreKey.NormalizeId(id);
        return WithGateAsync(() => localStoreService.SetAsync(Group, normalizedId, value, cancellationToken), cancellationToken);
    }

    public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        return SetAsync(id, null, cancellationToken);
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
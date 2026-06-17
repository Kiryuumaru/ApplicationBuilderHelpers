using Application.LocalStore.Interfaces.Outbound;
using Infrastructure.Browser.IndexedDB.LocalStore.Models;
using Infrastructure.Browser.IndexedDB.LocalStore.Serialization;
using Microsoft.JSInterop;
using System.Text.Json;

namespace Infrastructure.Browser.IndexedDB.LocalStore.Services;

internal sealed class IndexedDBLocalStoreService(IJSRuntime jsRuntime) : ILocalStoreService, IAsyncDisposable
{
    private const string DatabaseName = "LocalStoreDB";
    private const string StoreName = "LocalStore";
    private const int DatabaseVersion = 1;

    // Safe: Assembly.GetName().Name is always non-null for loaded assemblies
    private static readonly string AssemblyName = typeof(IndexedDBLocalStoreService).Assembly.GetName().Name!;

    private readonly Lazy<Task<IJSObjectReference>> _moduleTask = new(() =>
        jsRuntime.InvokeAsync<IJSObjectReference>(
            "import", $"./_content/{AssemblyName}/indexedDBLocalStore.js").AsTask());

    private readonly List<PendingOperation> _pendingOperations = [];
    private bool _isOpen;
    private bool _disposed;

    public Task Open(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        _isOpen = true;
        return Task.CompletedTask;
    }

    public async Task CommitAsync(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();

        if (_pendingOperations.Count == 0)
        {
            return;
        }

        var operations = _pendingOperations.ToArray();
        _pendingOperations.Clear();

        var jsOperations = operations.Select(o => new JsOperation(
            o.Type == OperationType.Set ? "set" : "delete",
            o.Group,
            o.Id,
            o.Data
        )).ToArray();

        var operationsJson = JsonSerializer.Serialize(jsOperations, IndexedDBJsonContext.Default.JsOperationArray);

        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync(
            "commitOperationsJson",
            cancellationToken,
            DatabaseName,
            StoreName,
            DatabaseVersion,
            operationsJson);
    }

    public Task RollbackAsync(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        _pendingOperations.Clear();
        return Task.CompletedTask;
    }

    public async Task<string?> Get(string group, string id, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        await EnsureOpenAsync(cancellationToken);

        // Check pending operations first (newest to oldest)
        for (int i = _pendingOperations.Count - 1; i >= 0; i--)
        {
            var op = _pendingOperations[i];
            if (op.Group == group && op.Id == id)
            {
                return op.Type == OperationType.Delete ? null : op.Data;
            }
        }

        var module = await _moduleTask.Value;
        return await module.InvokeAsync<string?>(
            "get",
            cancellationToken,
            DatabaseName,
            StoreName,
            DatabaseVersion,
            group,
            id);
    }

    public async Task<string[]> GetIds(string group, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        await EnsureOpenAsync(cancellationToken);

        var module = await _moduleTask.Value;
        var idsFromDb = await module.InvokeAsync<string[]>(
            "getIds",
            cancellationToken,
            DatabaseName,
            StoreName,
            DatabaseVersion,
            group);

        // Apply pending operations to the result
        var resultSet = new HashSet<string>(idsFromDb);

        foreach (var op in _pendingOperations.Where(o => o.Group == group))
        {
            if (op.Type == OperationType.Delete)
            {
                resultSet.Remove(op.Id);
            }
            else
            {
                resultSet.Add(op.Id);
            }
        }

        return [.. resultSet];
    }

    public Task Set(string group, string id, string? data, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();

        if (data == null)
        {
            _pendingOperations.Add(new PendingOperation(OperationType.Delete, group, id, null));
        }
        else
        {
            _pendingOperations.Add(new PendingOperation(OperationType.Set, group, id, data));
        }

        return Task.CompletedTask;
    }

    public async Task<bool> Contains(string group, string id, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        await EnsureOpenAsync(cancellationToken);

        // Check pending operations first (newest to oldest)
        for (int i = _pendingOperations.Count - 1; i >= 0; i--)
        {
            var op = _pendingOperations[i];
            if (op.Group == group && op.Id == id)
            {
                return op.Type != OperationType.Delete;
            }
        }

        var module = await _moduleTask.Value;
        return await module.InvokeAsync<bool>(
            "contains",
            cancellationToken,
            DatabaseName,
            StoreName,
            DatabaseVersion,
            group,
            id);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _pendingOperations.Clear();
        _disposed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        if (_moduleTask.IsValueCreated)
        {
            var module = await _moduleTask.Value;
            await module.DisposeAsync();
        }

        _pendingOperations.Clear();
        _disposed = true;
    }

    private Task EnsureOpenAsync(CancellationToken cancellationToken)
    {
        if (!_isOpen)
        {
            return Open(cancellationToken);
        }
        return Task.CompletedTask;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private enum OperationType
    {
        Set,
        Delete
    }

    private sealed record PendingOperation(OperationType Type, string Group, string Id, string? Data);
}

using Application.LocalStore.Interfaces.Outbound;

namespace Infrastructure.EFCore.Services;

internal sealed class EFCoreDatabaseInitializationState : IDatabaseInitializationState
{
    private readonly TaskCompletionSource _initializationComplete = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public bool IsInitialized => _initializationComplete.Task.IsCompleted;

    public Task WaitForInitializationAsync(CancellationToken cancellationToken = default)
    {
        return _initializationComplete.Task.WaitAsync(cancellationToken);
    }

    internal void MarkInitialized()
    {
        _initializationComplete.TrySetResult();
    }
}

namespace Application.LocalStore.Interfaces.Outbound;

public interface IDatabaseInitializationState
{
    bool IsInitialized { get; }
    Task WaitForInitializationAsync(CancellationToken cancellationToken = default);
}

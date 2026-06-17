namespace Application.LocalStore.Interfaces.Outbound;

/// <summary>
/// Tracks database initialization state for coordinating startup dependencies.
/// </summary>
public interface IDatabaseInitializationState
{
    bool IsInitialized { get; }
    Task WaitForInitializationAsync(CancellationToken cancellationToken = default);
}

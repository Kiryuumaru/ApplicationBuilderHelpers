namespace Application.LocalStore.Interfaces.Inbound;

/// <summary>
/// Factory for creating concurrent local store instances.
/// </summary>
public interface ILocalStoreFactory
{
    Task<IConcurrentLocalStore> OpenStoreAsync(string group = "common_group", CancellationToken cancellationToken = default);
}

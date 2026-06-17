namespace Application.LocalStore.Interfaces.Inbound;

public interface ILocalStoreFactory
{
    Task<IConcurrentLocalStore> OpenStore(string group = "common_group", CancellationToken cancellationToken = default);
}

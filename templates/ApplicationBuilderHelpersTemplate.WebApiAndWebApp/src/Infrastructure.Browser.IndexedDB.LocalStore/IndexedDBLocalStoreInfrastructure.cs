using ApplicationBuilderHelpers;
using Infrastructure.Browser.IndexedDB.LocalStore.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Browser.IndexedDB.LocalStore;

public class IndexedDBLocalStoreInfrastructure : ApplicationDependency
{
    public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
    {
        base.AddServices(applicationBuilder, services);

        services.AddIndexedDBLocalStore();
    }
}

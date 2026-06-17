using Application.LocalStore.Interfaces.Outbound;
using Infrastructure.Browser.IndexedDB.LocalStore.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Browser.IndexedDB.LocalStore.Extensions;

internal static class IndexedDBLocalStoreServiceCollectionExtensions
{
    public static IServiceCollection AddIndexedDBLocalStore(this IServiceCollection services)
    {
        services.AddScoped<ILocalStoreService, IndexedDBLocalStoreService>();
        return services;
    }
}

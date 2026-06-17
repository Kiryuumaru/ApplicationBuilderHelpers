using Application.LocalStore.Interfaces.Inbound;
using Application.LocalStore.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Application.LocalStore.Extensions;

internal static class LocalStoreServiceCollectionExtensions
{
    /// <summary>
    /// Registers LocalStoreFactory. Note: ILocalStoreService must be registered by Infrastructure layer.
    /// </summary>
    public static IServiceCollection AddLocalStoreServices(this IServiceCollection services)
    {
        services.AddScoped<ILocalStoreFactory, LocalStoreFactory>();
        return services;
    }
}

using Application.LocalStore.Interfaces.Outbound;
using Infrastructure.EFCore.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.EFCore.Extensions;

internal static class EFCoreInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddEFCoreInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<EFCoreDatabaseInitializationState>();
        services.AddSingleton<IDatabaseInitializationState>(sp => sp.GetRequiredService<EFCoreDatabaseInitializationState>());
        
        return services;
    }
}

using Microsoft.Extensions.DependencyInjection;

namespace Domain.AppEnvironment.Extensions;

internal static class AppEnvironmentServiceCollectionExtensions
{
    internal static IServiceCollection AddAppEnvironmentServices(this IServiceCollection services)
    {
        return services;
    }
}

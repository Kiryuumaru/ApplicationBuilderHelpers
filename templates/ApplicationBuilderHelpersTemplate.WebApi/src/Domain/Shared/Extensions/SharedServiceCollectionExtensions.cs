using Microsoft.Extensions.DependencyInjection;

namespace Domain.Shared.Extensions;

internal static class SharedServiceCollectionExtensions
{
    internal static IServiceCollection AddSharedServices(this IServiceCollection services)
    {
        return services;
    }
}

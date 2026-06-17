using Microsoft.Extensions.DependencyInjection;

namespace Application.Shared.Extensions;

public static class SharedServiceCollectionExtensions
{
    internal static IServiceCollection AddSharedServices(this IServiceCollection services)
    {
        return services;
    }
}

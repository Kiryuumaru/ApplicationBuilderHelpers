using Application.EmbeddedConfig.Interfaces.Inbound;
using Application.EmbeddedConfig.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Application.EmbeddedConfig.Extensions;

internal static class EmbeddedConfigServiceCollectionExtensions
{
    internal static IServiceCollection AddEmbeddedConfigServices(this IServiceCollection services)
    {
        services.AddScoped<IEmbeddedConfigService, EmbeddedConfigService>();

        return services;
    }
}

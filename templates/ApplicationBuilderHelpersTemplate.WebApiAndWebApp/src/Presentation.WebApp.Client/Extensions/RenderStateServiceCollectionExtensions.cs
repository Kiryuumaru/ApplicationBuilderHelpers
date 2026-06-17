using Microsoft.Extensions.DependencyInjection;
using Presentation.WebApp.Client.Services;
using Presentation.WebApp.Services;

namespace Presentation.WebApp.Client.Extensions;

internal static class RenderStateServiceCollectionExtensions
{
    public static IServiceCollection AddClientRenderStateServices(this IServiceCollection services)
    {
        services.AddScoped<IRenderStateService, ClientRenderStateService>();
        return services;
    }
}

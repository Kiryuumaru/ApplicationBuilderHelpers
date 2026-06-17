using Microsoft.Extensions.DependencyInjection;
using Presentation.WebApp.Server.Services;
using Presentation.WebApp.Services;

namespace Presentation.WebApp.Server.Extensions;

/// <summary>
/// Extension methods for registering server-side render state services.
/// </summary>
internal static class RenderStateServiceCollectionExtensions
{
    /// <summary>
    /// Adds the server-side render state service to the service collection.
    /// This service detects pre-rendering vs SSR mode on the server.
    /// </summary>
    internal static IServiceCollection AddServerRenderStateServices(this IServiceCollection services)
    {
        services.AddScoped<IRenderStateService, ServerRenderStateService>();
        return services;
    }
}

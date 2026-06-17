using Presentation.WebApp.Client.Components.Theme.Interfaces;
using Presentation.WebApp.Client.Components.Theme.Services;

namespace Presentation.WebApp.Client.Components.Theme.Extensions;

internal static class ThemeServiceCollectionExtensions
{
    public static IServiceCollection AddThemeServices(this IServiceCollection services)
    {
        services.AddScoped<IThemeService, ThemeService>();
        return services;
    }
}

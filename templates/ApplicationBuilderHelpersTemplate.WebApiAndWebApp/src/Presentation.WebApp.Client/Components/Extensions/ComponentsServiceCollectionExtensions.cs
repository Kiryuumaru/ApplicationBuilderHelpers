using Presentation.WebApp.Client.Components.Notifications.Extensions;
using Presentation.WebApp.Client.Components.Theme.Extensions;
using Presentation.WebApp.Client.Components.Theme.Interfaces;
using Presentation.WebApp.Client.Components.Theme.Services;

namespace Presentation.WebApp.Client.Components.Extensions;

internal static class ComponentsServiceCollectionExtensions
{
    public static IServiceCollection AddClientComponents(this IServiceCollection services)
    {
        services.AddThemeServices();
        services.AddNotificationComponentServices();

        return services;
    }
}

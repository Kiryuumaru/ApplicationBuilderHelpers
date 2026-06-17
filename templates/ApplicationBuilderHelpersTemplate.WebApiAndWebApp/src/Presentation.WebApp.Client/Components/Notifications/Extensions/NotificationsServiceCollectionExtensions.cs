using Presentation.WebApp.Client.Components.Notifications.Interfaces;
using Presentation.WebApp.Client.Components.Notifications.Services;

namespace Presentation.WebApp.Client.Components.Notifications.Extensions;

/// <summary>
/// Extension methods for registering notification component services.
/// </summary>
internal static class NotificationsServiceCollectionExtensions
{
    /// <summary>
    /// Adds notification component services (toast and dialog) to the service collection.
    /// </summary>
    internal static IServiceCollection AddNotificationComponentServices(this IServiceCollection services)
    {
        services.AddSingleton<ToastService>();
        services.AddSingleton<IToastService>(sp => sp.GetRequiredService<ToastService>());

        services.AddSingleton<ConfirmDialogService>();
        services.AddSingleton<IConfirmDialogService>(sp => sp.GetRequiredService<ConfirmDialogService>());

        services.AddSingleton<AlertDialogService>();
        services.AddSingleton<IAlertDialogService>(sp => sp.GetRequiredService<AlertDialogService>());

        services.AddSingleton<PromptDialogService>();
        services.AddSingleton<IPromptDialogService>(sp => sp.GetRequiredService<PromptDialogService>());

        return services;
    }
}

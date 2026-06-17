using Presentation.WebApp.Client.Components.Notifications.Interfaces;
using Presentation.WebApp.Client.Components.Notifications.Models;

namespace Presentation.WebApp.Client.Components.Notifications.Services;

internal sealed class AlertDialogService : IAlertDialogService
{
    internal event Action<AlertDialogRequest>? OnAlertRequested;

    public Task AlertAsync(
        string message,
        string? title = null,
        string okText = "OK")
    {
        var tcs = new TaskCompletionSource();
        var request = new AlertDialogRequest(message, title, okText, tcs);
        OnAlertRequested?.Invoke(request);
        return tcs.Task;
    }
}

using Presentation.WebApp.Client.Components.Notifications.Enums;
using Presentation.WebApp.Client.Components.Notifications.Interfaces;
using Presentation.WebApp.Client.Components.Notifications.Models;

namespace Presentation.WebApp.Client.Components.Notifications.Services;

internal sealed class ConfirmDialogService : IConfirmDialogService
{
    internal event Action<ConfirmDialogRequest>? OnConfirmRequested;

    public Task<bool> ConfirmAsync(
        string message,
        string? title = null,
        string confirmText = "Confirm",
        string cancelText = "Cancel",
        DialogVariant variant = DialogVariant.Default)
    {
        var tcs = new TaskCompletionSource<bool>();
        var request = new ConfirmDialogRequest(message, title, confirmText, cancelText, variant, tcs);
        OnConfirmRequested?.Invoke(request);
        return tcs.Task;
    }
}

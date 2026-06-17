using Presentation.WebApp.Client.Components.Notifications.Interfaces;
using Presentation.WebApp.Client.Components.Notifications.Models;

namespace Presentation.WebApp.Client.Components.Notifications.Services;

internal sealed class PromptDialogService : IPromptDialogService
{
    internal event Action<PromptDialogRequest>? OnPromptRequested;

    public Task<string?> PromptAsync(
        string message,
        string? title = null,
        string? defaultValue = null,
        string? placeholder = null,
        string confirmText = "OK",
        string cancelText = "Cancel")
    {
        var tcs = new TaskCompletionSource<string?>();
        var request = new PromptDialogRequest(message, title, defaultValue, placeholder, confirmText, cancelText, tcs);
        OnPromptRequested?.Invoke(request);
        return tcs.Task;
    }
}

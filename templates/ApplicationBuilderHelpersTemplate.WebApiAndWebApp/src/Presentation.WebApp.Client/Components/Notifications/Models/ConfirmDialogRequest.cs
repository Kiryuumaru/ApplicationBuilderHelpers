using Presentation.WebApp.Client.Components.Notifications.Enums;

namespace Presentation.WebApp.Client.Components.Notifications.Models;

/// <summary>
/// Request data for a confirmation dialog.
/// </summary>
/// <param name="Message">The message to display.</param>
/// <param name="Title">Optional dialog title.</param>
/// <param name="ConfirmText">Text for the confirm button.</param>
/// <param name="CancelText">Text for the cancel button.</param>
/// <param name="Variant">Visual styling variant.</param>
/// <param name="TaskCompletionSource">Completion source for the async result.</param>
public record ConfirmDialogRequest(
    string Message,
    string? Title,
    string ConfirmText,
    string CancelText,
    DialogVariant Variant,
    TaskCompletionSource<bool> TaskCompletionSource
);

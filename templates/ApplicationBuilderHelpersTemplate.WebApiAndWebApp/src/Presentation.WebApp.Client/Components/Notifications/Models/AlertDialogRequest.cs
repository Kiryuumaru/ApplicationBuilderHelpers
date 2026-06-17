namespace Presentation.WebApp.Client.Components.Notifications.Models;

/// <summary>
/// Request data for an alert dialog.
/// </summary>
/// <param name="Message">The message to display.</param>
/// <param name="Title">Optional dialog title.</param>
/// <param name="OkText">Text for the OK button.</param>
/// <param name="TaskCompletionSource">Completion source for the async result.</param>
public record AlertDialogRequest(
    string Message,
    string? Title,
    string OkText,
    TaskCompletionSource TaskCompletionSource
);

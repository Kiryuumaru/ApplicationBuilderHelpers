namespace Presentation.WebApp.Client.Components.Notifications.Models;

/// <summary>
/// Request data for a prompt dialog.
/// </summary>
/// <param name="Message">The message to display.</param>
/// <param name="Title">Optional dialog title.</param>
/// <param name="DefaultValue">Default value for the input.</param>
/// <param name="Placeholder">Placeholder text for the input.</param>
/// <param name="ConfirmText">Text for the confirm button.</param>
/// <param name="CancelText">Text for the cancel button.</param>
/// <param name="TaskCompletionSource">Completion source for the async result.</param>
public record PromptDialogRequest(
    string Message,
    string? Title,
    string? DefaultValue,
    string? Placeholder,
    string ConfirmText,
    string CancelText,
    TaskCompletionSource<string?> TaskCompletionSource
);

namespace Presentation.WebApp.Client.Components.Notifications.Interfaces;

/// <summary>
/// Service for displaying prompt dialogs.
/// </summary>
public interface IPromptDialogService
{
    /// <summary>
    /// Shows a prompt dialog and returns the user's input.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <param name="title">Optional dialog title.</param>
    /// <param name="defaultValue">Default value in the input field.</param>
    /// <param name="placeholder">Placeholder text for the input field.</param>
    /// <param name="confirmText">Text for the confirm button.</param>
    /// <param name="cancelText">Text for the cancel button.</param>
    /// <returns>The entered value, or null if cancelled.</returns>
    Task<string?> PromptAsync(
        string message,
        string? title = null,
        string? defaultValue = null,
        string? placeholder = null,
        string confirmText = "OK",
        string cancelText = "Cancel");
}

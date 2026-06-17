namespace Presentation.WebApp.Client.Components.Notifications.Interfaces;

/// <summary>
/// Service for displaying alert dialogs.
/// </summary>
public interface IAlertDialogService
{
    /// <summary>
    /// Shows an alert dialog.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <param name="title">Optional dialog title.</param>
    /// <param name="okText">Text for the OK button.</param>
    Task AlertAsync(
        string message,
        string? title = null,
        string okText = "OK");
}

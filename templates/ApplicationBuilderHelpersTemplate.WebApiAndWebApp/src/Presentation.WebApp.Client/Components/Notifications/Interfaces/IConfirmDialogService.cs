using Presentation.WebApp.Client.Components.Notifications.Enums;
using Presentation.WebApp.Client.Components.Notifications.Models;

namespace Presentation.WebApp.Client.Components.Notifications.Interfaces;

/// <summary>
/// Service for displaying confirmation dialogs.
/// </summary>
public interface IConfirmDialogService
{
    /// <summary>
    /// Shows a confirmation dialog and returns the user's choice.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <param name="title">Optional dialog title.</param>
    /// <param name="confirmText">Text for the confirm button.</param>
    /// <param name="cancelText">Text for the cancel button.</param>
    /// <param name="variant">Visual variant of the dialog.</param>
    /// <returns>True if confirmed, false if cancelled.</returns>
    Task<bool> ConfirmAsync(
        string message,
        string? title = null,
        string confirmText = "Confirm",
        string cancelText = "Cancel",
        DialogVariant variant = DialogVariant.Default);
}

using Presentation.WebApp.Client.Components.Notifications.Models;

namespace Presentation.WebApp.Client.Components.Notifications.Interfaces;

/// <summary>
/// Service for displaying toast notifications.
/// </summary>
public interface IToastService
{
    /// <summary>Shows a success toast.</summary>
    void Success(string message, string? title = null, int durationMs = 5000);

    /// <summary>Shows an error toast.</summary>
    void Error(string message, string? title = null, int durationMs = 5000);

    /// <summary>Shows a warning toast.</summary>
    void Warning(string message, string? title = null, int durationMs = 5000);

    /// <summary>Shows an info toast.</summary>
    void Info(string message, string? title = null, int durationMs = 5000);

    /// <summary>Event raised when a toast is added.</summary>
    event Action<ToastMessage>? OnToastAdded;

    /// <summary>Event raised when a toast is removed.</summary>
    event Action<Guid>? OnToastRemoved;
}

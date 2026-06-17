namespace Presentation.WebApp.Client.Components.Notifications.Models;

/// <summary>
/// Represents a toast notification message.
/// </summary>
/// <param name="Id">Unique identifier for the toast.</param>
/// <param name="Type">Type of toast (success, error, warning, info).</param>
/// <param name="Message">The message content.</param>
/// <param name="Title">Optional title.</param>
/// <param name="DurationMs">Display duration in milliseconds.</param>
/// <param name="CreatedAt">When the toast was created.</param>
public record ToastMessage(
    Guid Id,
    ToastType Type,
    string Message,
    string? Title,
    int DurationMs,
    DateTimeOffset CreatedAt
);

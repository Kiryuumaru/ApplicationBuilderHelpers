namespace Presentation.WebApp.Client.Components.Theme.Interfaces;

/// <summary>
/// Service for managing UI theme (light/dark mode).
/// </summary>
public interface IThemeService
{
    /// <summary>
    /// Event raised when the theme changes.
    /// </summary>
    event Action<bool>? ThemeChanged;

    /// <summary>
    /// Gets whether dark mode is currently active.
    /// </summary>
    Task<bool> IsDarkModeAsync();

    /// <summary>
    /// Sets the theme to dark or light mode.
    /// </summary>
    /// <param name="isDark">True for dark mode, false for light mode.</param>
    Task SetDarkModeAsync(bool isDark);

    /// <summary>
    /// Toggles between dark and light mode.
    /// </summary>
    /// <returns>True if dark mode is now active, false otherwise.</returns>
    Task<bool> ToggleDarkModeAsync();

    /// <summary>
    /// Initializes the theme from stored preference or system default.
    /// Should be called once on app startup.
    /// </summary>
    Task InitializeAsync();
}

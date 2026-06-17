using Application.LocalStore.Interfaces.Outbound;
using Microsoft.JSInterop;
using Presentation.WebApp.Client.Components.Theme.Interfaces;

namespace Presentation.WebApp.Client.Components.Theme.Services;

internal sealed class ThemeService(IJSRuntime jsRuntime, ILocalStoreService localStore) : IThemeService
{
    private const string ThemeStorageGroup = "theme";
    private const string ThemeStorageId = "preference";
    private const string DarkClass = "dark";

    private bool? _cachedIsDark;

    public event Action<bool>? ThemeChanged;

    public async Task<bool> IsDarkModeAsync()
    {
        if (_cachedIsDark.HasValue)
        {
            return _cachedIsDark.Value;
        }

        _cachedIsDark = await jsRuntime.InvokeAsync<bool>(
            "eval",
            $"document.documentElement.classList.contains('{DarkClass}')"
        );

        return _cachedIsDark.Value;
    }

    public async Task SetDarkModeAsync(bool isDark)
    {
        var action = isDark ? "add" : "remove";
        await jsRuntime.InvokeVoidAsync(
            "eval",
            $"document.documentElement.classList.{action}('{DarkClass}')"
        );

        _cachedIsDark = isDark;
        
        await localStore.Open(CancellationToken.None);
        await localStore.Set(ThemeStorageGroup, ThemeStorageId, isDark.ToString(), CancellationToken.None);
        await localStore.CommitAsync(CancellationToken.None);

        ThemeChanged?.Invoke(isDark);
    }

    public async Task<bool> ToggleDarkModeAsync()
    {
        var isDark = await IsDarkModeAsync();
        var newValue = !isDark;
        await SetDarkModeAsync(newValue);
        return newValue;
    }

    public async Task InitializeAsync()
    {
        bool isDark;

        try
        {
            await localStore.Open(CancellationToken.None);
            var stored = await localStore.Get(ThemeStorageGroup, ThemeStorageId, CancellationToken.None);
            
            if (!string.IsNullOrEmpty(stored) && bool.TryParse(stored, out var parsed))
            {
                isDark = parsed;
            }
            else
            {
                // Fall back to system preference
                isDark = await jsRuntime.InvokeAsync<bool>(
                    "eval",
                    "window.matchMedia('(prefers-color-scheme: dark)').matches"
                );
            }
        }
        catch
        {
            // Default to light mode if storage fails
            isDark = false;
        }

        await SetDarkModeAsync(isDark);
    }
}

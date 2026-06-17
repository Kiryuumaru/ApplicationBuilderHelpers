using Microsoft.Playwright;

namespace Presentation.WebApp.Client.FunctionalTests.Fixtures;

/// <summary>
/// Fixture that manages Playwright browser lifecycle.
/// </summary>
public class PlaywrightFixture : IAsyncLifetime
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public IBrowser Browser => _browser ?? throw new InvalidOperationException("Browser not initialized");
    public IPlaywright Playwright => _playwright ?? throw new InvalidOperationException("Playwright not initialized");

    public async Task InitializeAsync()
    {
        Console.WriteLine("[PLAYWRIGHT] Initializing...");

        // Install browsers if needed
        var exitCode = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        if (exitCode != 0)
        {
            throw new InvalidOperationException($"Playwright browser installation failed with exit code {exitCode}");
        }

        _playwright = await Microsoft.Playwright.Playwright.CreateAsync();

        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true, // Set to false to see the browser during debugging
            SlowMo = 0       // Set to 100+ to slow down operations for debugging
        });

        Console.WriteLine("[PLAYWRIGHT] Browser launched");
    }

    public async Task DisposeAsync()
    {
        Console.WriteLine("[PLAYWRIGHT] Disposing...");

        if (_browser != null)
        {
            await _browser.CloseAsync();
        }

        _playwright?.Dispose();

        Console.WriteLine("[PLAYWRIGHT] Disposed");
    }

    /// <summary>
    /// Creates a new browser context with isolated state.
    /// </summary>
    public async Task<IBrowserContext> CreateContextAsync()
    {
        return await Browser.NewContextAsync(new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = true,
            ViewportSize = new ViewportSize { Width = 1280, Height = 720 }
        });
    }
}

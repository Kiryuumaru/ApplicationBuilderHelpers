using Microsoft.Playwright;
using Presentation.WebApp.Client.FunctionalTests.Fixtures;

namespace Presentation.WebApp.Client.FunctionalTests;

/// <summary>
/// Base class for WebApp functional tests with COMPLETE TEST ISOLATION.
/// 
/// Each test gets:
/// - Its OWN WebApiTestHost (unique port + unique in-memory SQLite database)
/// - Its own browser context (via shared PlaywrightFixture - browser is OK to share)
/// 
/// This ensures ZERO cross-test interference. No shared state. No flaky tests.
/// 
/// IMPORTANT: All tests MUST use UI-only interactions (click, type, navigate).
/// NO direct API calls. NO manual header injection. Like a real user.
/// </summary>
[Collection(WebAppTestCollection.Name)]
public abstract class WebAppTestBase : IAsyncLifetime
{
    /// <summary>
    /// Default timeout for Playwright operations in milliseconds (15 seconds).
    /// </summary>
    protected const int DefaultTimeoutMs = 15_000;

    private readonly PlaywrightFixture _playwrightFixture;
    private WebApiTestHost? _host;
    private IBrowserContext? _context;
    private IPage? _page;

    /// <summary>
    /// Test output helper for logging.
    /// </summary>
    protected readonly ITestOutputHelper Output;

    /// <summary>
    /// Base URL for the WebApp server (unique per test).
    /// </summary>
    protected string WebAppUrl => _host?.BaseUrl ?? throw new InvalidOperationException("Host not initialized");

    /// <summary>
    /// The browser context for this test.
    /// </summary>
    protected IBrowserContext Context => _context ?? throw new InvalidOperationException("Browser context not initialized");

    /// <summary>
    /// The browser page for this test.
    /// </summary>
    protected IPage Page => _page ?? throw new InvalidOperationException("Page not initialized");

    /// <summary>
    /// Standard test password used across tests.
    /// </summary>
    protected const string TestPassword = "TestPassword123!";

    protected WebAppTestBase(PlaywrightFixture playwrightFixture, ITestOutputHelper output)
    {
        _playwrightFixture = playwrightFixture;
        Output = output;
    }

    public virtual async Task InitializeAsync()
    {
        Output.WriteLine("[TEST] Starting isolated WebApiTestHost...");

        // Create ISOLATED host for THIS TEST ONLY (unique port + unique in-memory DB)
        _host = new WebApiTestHost(Output);
        await _host.StartAsync();

        Output.WriteLine($"[TEST] Host started at {_host.BaseUrl}");
        Output.WriteLine("[TEST] Creating browser context...");

        // Create isolated browser context from shared Playwright fixture
        _context = await _playwrightFixture.CreateContextAsync();
        _page = await _context.NewPageAsync();

        // Set default timeouts
        _page.SetDefaultTimeout(DefaultTimeoutMs);
        _page.SetDefaultNavigationTimeout(DefaultTimeoutMs);

        // Log browser console messages
        _page.Console += (_, msg) =>
        {
            Output.WriteLine($"[BROWSER] {msg.Type}: {msg.Text}");
        };

        // Log page errors
        _page.PageError += (_, error) =>
        {
            Output.WriteLine($"[BROWSER ERROR] {error}");
        };

        Output.WriteLine($"[TEST] Browser context ready. WebApp at {WebAppUrl}");
    }

    public virtual async Task DisposeAsync()
    {
        Output.WriteLine("[TEST] Disposing test resources...");

        if (_page != null)
        {
            await _page.CloseAsync();
        }

        if (_context != null)
        {
            await _context.CloseAsync();
        }

        // Dispose the isolated host for this test
        if (_host != null)
        {
            await _host.DisposeAsync();
        }

        Output.WriteLine("[TEST] Test resources disposed");
    }

    #region Navigation Helpers (UI-Only)

    /// <summary>
    /// Navigate to the WebApp home page by URL.
    /// </summary>
    protected async Task GoToHomeAsync()
    {
        await Page.GotoAsync(WebAppUrl);
        await WaitForBlazorAsync();
    }

    /// <summary>
    /// Navigate to the login page by URL.
    /// </summary>
    protected async Task GoToLoginAsync()
    {
        await Page.GotoAsync($"{WebAppUrl}/auth/login");
        await WaitForBlazorAsync();
    }

    /// <summary>
    /// Navigate to the register page by URL.
    /// </summary>
    protected async Task GoToRegisterAsync()
    {
        await Page.GotoAsync($"{WebAppUrl}/auth/register");
        await WaitForBlazorAsync();
    }

    /// <summary>
    /// Click on a navigation link to go to Profile page.
    /// Must be authenticated first.
    /// </summary>
    protected async Task ClickNavigateToProfileAsync()
    {
        Output.WriteLine("[TEST] Clicking to navigate to Profile...");
        
        // Click user avatar/menu button to open dropdown
        // The button contains a div with rounded-full and user initial letter
        await Page.Locator("button:has(.rounded-full)").First.ClickAsync();
        await Task.Delay(300); // Wait for dropdown animation
        
        // Click Profile link (in the dropdown menu)
        await Page.Locator("a[href*='profile']").First.ClickAsync();
        
        await WaitForUrlContainsAsync("/account/profile");
        await WaitForBlazorAsync();
        
        Output.WriteLine($"[TEST] Navigated to: {Page.Url}");
    }

    /// <summary>
    /// Click on a navigation link to go to Change Password page.
    /// Must be authenticated first.
    /// </summary>
    protected async Task ClickNavigateToChangePasswordAsync()
    {
        Output.WriteLine("[TEST] Clicking to navigate to Change Password...");
        
        // Click user avatar/menu button to open dropdown
        await Page.Locator("button:has(.rounded-full)").First.ClickAsync();
        await Task.Delay(300);
        
        // Click Change Password or Settings link
        await Page.Locator("a[href*='settings'], a[href*='change-password']").First.ClickAsync();
        
        await WaitForBlazorAsync();
        
        Output.WriteLine($"[TEST] Navigated to: {Page.Url}");
    }

    #endregion

    #region Authentication Helpers (UI-Only - Click + Type Only)

    /// <summary>
    /// Register a new user account via the WebApp UI.
    /// Uses ONLY click and type actions - no direct API calls.
    /// </summary>
    protected async Task<bool> RegisterUserAsync(string username, string email, string password)
    {
        await GoToRegisterAsync();

        Output.WriteLine($"[TEST] Registering user via UI: {username}");

        // Type into registration form fields
        await Page.Locator("#username").FillAsync(username);
        await Page.Locator("#email").FillAsync(email);
        await Page.Locator("#password").FillAsync(password);
        await Page.Locator("#confirmPassword").FillAsync(password);

        // Check terms checkbox if it exists
        var termsCheckbox = Page.Locator("#terms");
        if (await termsCheckbox.CountAsync() > 0)
        {
            await termsCheckbox.CheckAsync();
        }

        // Click submit button
        Output.WriteLine("[TEST] Clicking submit button...");
        await Page.Locator("button[type='submit']").ClickAsync();

        // Wait for navigation away from register page
        var success = await WaitForUrlNotContainsAsync("/auth/register", timeoutMs: 20000);

        Output.WriteLine($"[TEST] Registration {(success ? "succeeded" : "failed")}. Current URL: {Page.Url}");

        return success;
    }

    /// <summary>
    /// Login with credentials via the WebApp UI.
    /// Uses ONLY click and type actions - no direct API calls.
    /// </summary>
    protected async Task<bool> LoginAsync(string email, string password)
    {
        await GoToLoginAsync();

        Output.WriteLine($"[TEST] Logging in via UI as: {email}");

        // Type into login form fields
        await Page.Locator("#email").FillAsync(email);
        await Page.Locator("#password").FillAsync(password);

        // Click submit button
        await Page.Locator("button[type='submit']").ClickAsync();

        // Wait for navigation away from login page
        var success = await WaitForUrlNotContainsAsync("/auth/login", timeoutMs: 20000);

        Output.WriteLine($"[TEST] Login {(success ? "succeeded" : "failed")}. Current URL: {Page.Url}");

        return success;
    }

    /// <summary>
    /// Logout via the WebApp UI.
    /// Uses ONLY click actions - no direct API calls.
    /// </summary>
    protected async Task LogoutAsync()
    {
        Output.WriteLine("[TEST] Logging out via UI...");

        // Click user avatar/menu button to open dropdown
        var userMenu = Page.Locator("button:has(.rounded-full)").First;
        if (await userMenu.CountAsync() > 0)
        {
            await userMenu.ClickAsync();
            await Task.Delay(300);
        }

        // Click Sign out button
        var signOut = Page.Locator("button:has-text('Sign out')").First;
        
        if (await signOut.CountAsync() > 0)
        {
            await signOut.ClickAsync();
        }
        else
        {
            // Fallback: navigate directly
            await Page.GotoAsync($"{WebAppUrl}/auth/logout");
        }

        await WaitForUrlContainsAsync("/auth/login", timeoutMs: 10000);
        await WaitForBlazorAsync();

        Output.WriteLine("[TEST] Logout completed");
    }

    /// <summary>
    /// Change password via the WebApp UI.
    /// Uses ONLY click and type actions - no direct API calls.
    /// </summary>
    protected async Task<bool> ChangePasswordAsync(string currentPassword, string newPassword)
    {
        Output.WriteLine("[TEST] Changing password via UI...");

        // Navigate to change password page
        await ClickNavigateToChangePasswordAsync();

        // Fill in the form
        await Page.Locator("#currentPassword").FillAsync(currentPassword);
        await Page.Locator("#newPassword").FillAsync(newPassword);
        await Page.Locator("#confirmPassword").FillAsync(newPassword);

        // Click submit
        await Page.Locator("button[type='submit']").ClickAsync();

        // Wait for success indicator
        var success = await WaitForSuccessMessageAsync(timeoutMs: 10000);

        Output.WriteLine($"[TEST] Change password {(success ? "succeeded" : "failed")}");

        return success;
    }

    /// <summary>
    /// Check if user is currently authenticated by looking for authenticated UI elements.
    /// </summary>
    protected async Task<bool> IsAuthenticatedAsync()
    {
        // Wait a bit for the UI to update after auth state changes
        await Task.Delay(500);

        // Look for user menu button (only visible when authenticated)
        // The button contains a div with rounded-full and user initial letter
        var userMenu = Page.Locator("button:has(.rounded-full)");
        if (await userMenu.CountAsync() > 0)
        {
            return true;
        }

        return false;
    }

    #endregion

    #region Wait Helpers

    /// <summary>
    /// Wait for a specific element to appear on the page.
    /// </summary>
    protected async Task<ILocator> WaitForAsync(string selector, int? timeoutMs = null)
    {
        var locator = Page.Locator(selector);
        await locator.WaitForAsync(new LocatorWaitForOptions { Timeout = timeoutMs ?? DefaultTimeoutMs });
        return locator;
    }

    /// <summary>
    /// Wait for an element containing specific text to appear.
    /// </summary>
    protected async Task<ILocator> WaitForTextAsync(string text, int? timeoutMs = null)
    {
        var locator = Page.GetByText(text);
        await locator.WaitForAsync(new LocatorWaitForOptions { Timeout = timeoutMs ?? DefaultTimeoutMs });
        return locator;
    }

    /// <summary>
    /// Wait for Blazor WASM to fully load and hydrate.
    /// </summary>
    protected async Task WaitForBlazorAsync(int timeoutMs = 30000)
    {
        // Wait for Blazor framework script
        await Page.WaitForSelectorAsync("script[src*='blazor.web.']", new PageWaitForSelectorOptions
        {
            State = WaitForSelectorState.Attached,
            Timeout = timeoutMs
        });

        // Wait for page content
        await Page.WaitForSelectorAsync("form, h1, main, nav, [data-page-content]", new PageWaitForSelectorOptions
        {
            State = WaitForSelectorState.Attached,
            Timeout = timeoutMs
        });

        // Wait for loading spinners to disappear
        try
        {
            await Page.WaitForSelectorAsync("[data-loading-spinner], .loading", new PageWaitForSelectorOptions
            {
                State = WaitForSelectorState.Hidden,
                Timeout = 5000
            });
        }
        catch (TimeoutException)
        {
            // Loading indicator might not exist
        }

        // Small delay for Blazor to settle
        await Task.Delay(100);
    }

    /// <summary>
    /// Wait for URL to contain a specific path.
    /// </summary>
    protected async Task<bool> WaitForUrlContainsAsync(string urlPart, int timeoutMs = 10000)
    {
        var elapsed = 0;
        var pollInterval = 100;

        while (elapsed < timeoutMs)
        {
            if (Page.Url.Contains(urlPart, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            await Task.Delay(pollInterval);
            elapsed += pollInterval;
        }

        return false;
    }

    /// <summary>
    /// Wait for URL to NOT contain a specific path.
    /// </summary>
    protected async Task<bool> WaitForUrlNotContainsAsync(string urlPart, int timeoutMs = 10000)
    {
        var elapsed = 0;
        var pollInterval = 100;

        while (elapsed < timeoutMs)
        {
            if (!Page.Url.Contains(urlPart, StringComparison.OrdinalIgnoreCase))
            {
                Output.WriteLine($"[TEST] URL no longer contains '{urlPart}' after {elapsed}ms. Current: {Page.Url}");
                return true;
            }
            await Task.Delay(pollInterval);
            elapsed += pollInterval;
        }

        Output.WriteLine($"[TEST] Timeout waiting for URL to not contain '{urlPart}'. Current: {Page.Url}");
        return false;
    }

    /// <summary>
    /// Wait for a success message to appear on the page.
    /// </summary>
    protected async Task<bool> WaitForSuccessMessageAsync(int timeoutMs = 10000)
    {
        try
        {
            await Page.Locator("[data-testid='success-message'], .alert-success, .text-green-600, [class*='success']")
                .First.WaitForAsync(new LocatorWaitForOptions { Timeout = timeoutMs });
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }

    /// <summary>
    /// Wait for an error message to appear on the page.
    /// </summary>
    protected async Task<bool> WaitForErrorMessageAsync(int timeoutMs = 5000)
    {
        try
        {
            await Page.Locator("[data-testid='error-message'], .alert-danger, .alert-error, .text-red-600, [class*='error']")
                .First.WaitForAsync(new LocatorWaitForOptions { Timeout = timeoutMs });
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }

    #endregion

    #region Assertion Helpers

    /// <summary>
    /// Assert that an element with the given text exists on the page.
    /// </summary>
    protected async Task AssertTextVisibleAsync(string text)
    {
        var locator = Page.GetByText(text);
        await locator.WaitForAsync(new LocatorWaitForOptions { Timeout = 5000 });
        Assert.True(await locator.CountAsync() > 0, $"Expected text '{text}' to be visible on page");
    }

    /// <summary>
    /// Assert that we're on a specific page path.
    /// </summary>
    protected void AssertUrlContains(string path)
    {
        Assert.Contains(path, Page.Url, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Assert that we're NOT on a specific page path.
    /// </summary>
    protected void AssertUrlDoesNotContain(string path)
    {
        Assert.DoesNotContain(path, Page.Url, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Assert that user is authenticated (sees authenticated UI elements).
    /// </summary>
    protected async Task AssertIsAuthenticatedAsync()
    {
        var isAuth = await IsAuthenticatedAsync();
        Assert.True(isAuth, "Expected user to be authenticated but no authenticated UI elements found");
    }

    /// <summary>
    /// Assert that user is NOT authenticated (sees login link, no user menu).
    /// </summary>
    protected async Task AssertIsNotAuthenticatedAsync()
    {
        var isAuth = await IsAuthenticatedAsync();
        Assert.False(isAuth, "Expected user to NOT be authenticated but found authenticated UI elements");
    }

    #endregion

    #region Test Data Helpers

    /// <summary>
    /// Generate a unique username for testing.
    /// </summary>
    protected static string GenerateUsername(string prefix = "user")
    {
        return $"{prefix}_{Guid.NewGuid():N}"[..20]; // Max 20 chars
    }

    /// <summary>
    /// Generate a unique email for testing.
    /// </summary>
    protected static string GenerateEmail(string username)
    {
        return $"{username}@test.example.com";
    }

    #endregion
}

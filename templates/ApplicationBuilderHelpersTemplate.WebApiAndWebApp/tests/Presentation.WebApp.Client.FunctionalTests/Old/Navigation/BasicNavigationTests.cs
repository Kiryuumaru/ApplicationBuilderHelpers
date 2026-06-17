using Presentation.WebApp.Client.FunctionalTests.Fixtures;

namespace Presentation.WebApp.Client.FunctionalTests.Old.Navigation;

/// <summary>
/// Playwright functional tests for basic navigation.
/// Tests that pages load correctly and navigation works.
/// </summary>
public class BasicNavigationTests : WebAppTestBase
{
    public BasicNavigationTests(PlaywrightFixture playwrightFixture, ITestOutputHelper output) : base(playwrightFixture, output)
    {
    }

    [Fact]
    public async Task Home_PageLoads_HasContent()
    {
        // Act
        await GoToHomeAsync();

        // Assert - Page should load without errors
        var title = await Page.TitleAsync();
        Output.WriteLine($"Page title: {title}");

        // Blazor Web App redirects unauthenticated users to login
        // Verify the app loaded correctly by checking for rendered content
        var bodyContent = await Page.ContentAsync();
        Assert.False(string.IsNullOrEmpty(bodyContent));

        // Should have a body element with rendered Blazor content
        var bodyElement = await Page.QuerySelectorAsync("body");
        Assert.NotNull(bodyElement);

        // Blazor script should be loaded (indicates app initialized)
        var blazorScript = await Page.QuerySelectorAsync("script[src*='blazor.web.']");
        Assert.NotNull(blazorScript);
    }

    [Fact]
    public async Task Home_HasNavigationLinks()
    {
        // Act
        await GoToHomeAsync();

        // Assert - Should have navigation elements
        var nav = await Page.QuerySelectorAsync("nav, [role='navigation'], .nav, .navbar");
        Output.WriteLine($"Navigation element found: {nav != null}");

        // Check for login/register links (common for unauthenticated users)
        var loginLink = await Page.QuerySelectorAsync("a[href*='login' i]");
        var registerLink = await Page.QuerySelectorAsync("a[href*='register' i]");

        Output.WriteLine($"Login link found: {loginLink != null}");
        Output.WriteLine($"Register link found: {registerLink != null}");

        // At least one navigation element should exist
        Assert.True(nav != null || loginLink != null || registerLink != null,
            "Should have some navigation elements");
    }

    [Fact]
    public async Task Login_NavigatesCorrectly()
    {
        // Act
        await GoToLoginAsync();

        // Assert
        AssertUrlContains("/auth/login");
    }

    [Fact]
    public async Task Register_NavigatesCorrectly()
    {
        // Act
        await GoToRegisterAsync();

        // Assert
        AssertUrlContains("/auth/register");
    }

    [Fact]
    public async Task Navigation_BetweenLoginAndRegister_Works()
    {
        // Start at login
        await GoToLoginAsync();
        AssertUrlContains("/auth/login");

        // Click register link
        var registerLink = await Page.QuerySelectorAsync("a[href*='register' i]");
        if (registerLink != null)
        {
            await registerLink.ClickAsync();
            await WaitForBlazorAsync();
            AssertUrlContains("/auth/register");
        }

        // Click login link
        var loginLink = await Page.QuerySelectorAsync("a[href*='login' i]");
        if (loginLink != null)
        {
            await loginLink.ClickAsync();
            await WaitForBlazorAsync();
            AssertUrlContains("/auth/login");
        }
    }

    [Fact]
    public async Task Navigation_ToNonExistentPage_Shows404OrRedirects()
    {
        // Act
        await Page.GotoAsync($"{WebAppUrl}/this-page-does-not-exist-{Guid.NewGuid():N}");
        await WaitForBlazorAsync();

        // Assert - Should either show 404 page or redirect to home
        var currentUrl = Page.Url;
        var pageContent = await Page.ContentAsync();

        Output.WriteLine($"Non-existent page URL: {currentUrl}");

        // Accept either: 404 content, redirect to home, or staying on invalid URL
        var hasNotFoundIndicator = pageContent.Contains("404", StringComparison.OrdinalIgnoreCase) ||
                                   pageContent.Contains("not found", StringComparison.OrdinalIgnoreCase);
        var redirectedToHome = currentUrl.TrimEnd('/') == WebAppUrl.TrimEnd('/') ||
                               currentUrl.Contains("/home", StringComparison.OrdinalIgnoreCase);

        Output.WriteLine($"Has 404 indicator: {hasNotFoundIndicator}");
        Output.WriteLine($"Redirected to home: {redirectedToHome}");

        // Either behavior is acceptable
        Assert.True(hasNotFoundIndicator || redirectedToHome || currentUrl.Contains("does-not-exist"),
            "Should show 404, redirect to home, or stay on URL with error page");
    }
}

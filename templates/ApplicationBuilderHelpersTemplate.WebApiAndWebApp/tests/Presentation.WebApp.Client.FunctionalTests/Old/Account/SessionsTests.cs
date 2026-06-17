using Presentation.WebApp.Client.FunctionalTests;
using Presentation.WebApp.Client.FunctionalTests.Fixtures;

namespace Presentation.WebApp.Client.FunctionalTests.Old.Account;

/// <summary>
/// Playwright functional tests for sessions management page.
/// Tests session viewing and revocation functionality.
/// </summary>
public class SessionsTests : WebAppTestBase
{
    public SessionsTests(PlaywrightFixture playwrightFixture, ITestOutputHelper output) : base(playwrightFixture, output)
    {
    }

    [Fact]
    public async Task Sessions_RequiresAuthentication()
    {
        // Act - Try to access sessions without authentication
        await Page.GotoAsync($"{WebAppUrl}/account/sessions");
        await WaitForBlazorAsync();

        // Assert - Should redirect to login or show unauthorized
        var currentUrl = Page.Url;
        var pageContent = await Page.ContentAsync();
        var redirectedToLogin = currentUrl.Contains("/auth/login", StringComparison.OrdinalIgnoreCase);
        var showsUnauthorized = pageContent.Contains("unauthorized", StringComparison.OrdinalIgnoreCase) ||
                                pageContent.Contains("access denied", StringComparison.OrdinalIgnoreCase) ||
                                pageContent.Contains("not authorized", StringComparison.OrdinalIgnoreCase);
        Assert.True(redirectedToLogin || showsUnauthorized, "Should redirect to login when accessing sessions unauthenticated");
    }

    [Fact]
    public async Task Sessions_Authenticated_ShowsSessionsList()
    {
        // Arrange - Register and login
        var username = $"sessions_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Navigate to sessions
        await Page.GotoAsync($"{WebAppUrl}/account/sessions");
        await WaitForBlazorAsync();

        // Assert - Should show sessions page
        var pageContent = await Page.ContentAsync();
        Output.WriteLine($"Sessions page content length: {pageContent.Length}");

        var hasSessionsContent = pageContent.Contains("session", StringComparison.OrdinalIgnoreCase) ||
                                 pageContent.Contains("device", StringComparison.OrdinalIgnoreCase) ||
                                 pageContent.Contains("active", StringComparison.OrdinalIgnoreCase);

        Assert.True(hasSessionsContent, "Sessions page should display session information");
    }

    [Fact]
    public async Task Sessions_ShowsCurrentSession()
    {
        // Arrange - Register and login
        var username = $"current_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Navigate to sessions
        await Page.GotoAsync($"{WebAppUrl}/account/sessions");
        await WaitForBlazorAsync();

        // Assert - Should show current session indicator
        var pageContent = await Page.ContentAsync();
        var hasCurrentIndicator = pageContent.Contains("current", StringComparison.OrdinalIgnoreCase) ||
                                  pageContent.Contains("this session", StringComparison.OrdinalIgnoreCase) ||
                                  pageContent.Contains("this device", StringComparison.OrdinalIgnoreCase);

        Output.WriteLine($"Has current session indicator: {hasCurrentIndicator}");
        Assert.True(hasCurrentIndicator, "Sessions page should indicate which session is the current one");
    }

    [Fact]
    public async Task Sessions_ShowsSessionDetails()
    {
        // Arrange - Register and login
        var username = $"details_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Navigate to sessions
        await Page.GotoAsync($"{WebAppUrl}/account/sessions");
        await WaitForBlazorAsync();

        // Assert - Should show session details
        var pageContent = await Page.ContentAsync();
        
        // Check for common session details
        var hasIpAddress = pageContent.Contains("IP", StringComparison.OrdinalIgnoreCase) ||
                          pageContent.Contains("address", StringComparison.OrdinalIgnoreCase);
        var hasUserAgent = pageContent.Contains("browser", StringComparison.OrdinalIgnoreCase) ||
                          pageContent.Contains("device", StringComparison.OrdinalIgnoreCase);
        var hasTimestamp = pageContent.Contains("created", StringComparison.OrdinalIgnoreCase) ||
                          pageContent.Contains("last", StringComparison.OrdinalIgnoreCase);

        Output.WriteLine($"Has IP address: {hasIpAddress}");
        Output.WriteLine($"Has user agent: {hasUserAgent}");
        Output.WriteLine($"Has timestamp: {hasTimestamp}");

        // Sessions should display at least some identifying information
        Assert.True(hasIpAddress || hasUserAgent || hasTimestamp,
            "Sessions page should display session details (IP, browser/device, or timestamps)");
    }

    [Fact]
    public async Task Sessions_HasRevokeButton()
    {
        // Arrange - Register and login
        var username = $"revoke_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Navigate to sessions
        await Page.GotoAsync($"{WebAppUrl}/account/sessions");
        await WaitForBlazorAsync();

        // Assert - Should have revoke button (for non-current sessions)
        var revokeButton = await Page.QuerySelectorAsync("button:has-text('Revoke'), button:has-text('Terminate'), button:has-text('End')");
        var pageContent = await Page.ContentAsync();
        var hasRevokeOption = revokeButton != null || 
                              pageContent.Contains("revoke", StringComparison.OrdinalIgnoreCase);

        Output.WriteLine($"Has revoke option: {hasRevokeOption}");
        Assert.True(hasRevokeOption, "Sessions page should have option to revoke sessions");
    }

    [Fact]
    public async Task Sessions_HasRevokeAllOtherSessionsButton()
    {
        // Arrange - Register and login
        var username = $"revokeall_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Navigate to sessions
        await Page.GotoAsync($"{WebAppUrl}/account/sessions");
        await WaitForBlazorAsync();

        // Assert - Should have revoke all button
        var revokeAllButton = await Page.QuerySelectorAsync("button:has-text('Revoke All'), button:has-text('Sign out all'), button:has-text('End All')");
        var pageContent = await Page.ContentAsync();
        var hasRevokeAllOption = revokeAllButton != null || 
                                 pageContent.Contains("revoke all", StringComparison.OrdinalIgnoreCase) ||
                                 pageContent.Contains("sign out all", StringComparison.OrdinalIgnoreCase);

        Output.WriteLine($"Has revoke all option: {hasRevokeAllOption}");
        Assert.True(hasRevokeAllOption, "Sessions page should have option to revoke all other sessions");
    }

    [Fact]
    public async Task Sessions_NavigationFromSidebar()
    {
        // Arrange - Register and login
        var username = $"nav_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Navigate via sidebar
        await GoToHomeAsync();
        var sessionsLink = await Page.QuerySelectorAsync("a[href*='sessions' i]");

        if (sessionsLink != null)
        {
            await sessionsLink.ClickAsync();
            await WaitForBlazorAsync();

            // Assert - Should be on sessions page
            AssertUrlContains("/account/sessions");
            Output.WriteLine("✅ Sessions page accessible via navigation");
        }
        else
        {
            Output.WriteLine("Sessions link not found in sidebar");
        }
    }
}

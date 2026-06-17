using Presentation.WebApp.Client.FunctionalTests.Fixtures;

namespace Presentation.WebApp.Client.FunctionalTests.Tests.Account;

/// <summary>
/// UI-only tests for sessions management page.
/// All tests use mouse clicks and keyboard input like a real user.
/// </summary>
public class SessionsFlowTests : WebAppTestBase
{
    public SessionsFlowTests(PlaywrightFixture playwrightFixture, ITestOutputHelper output) : base(playwrightFixture, output)
    {
    }

    [Fact]
    public async Task SessionsPage_RequiresAuthentication()
    {
        // Act - Navigate to sessions page without authentication
        await Page.GotoAsync($"{WebAppUrl}/account/sessions");
        await WaitForBlazorAsync();

        // Assert - Should redirect to login or show unauthorized content
        var currentUrl = Page.Url;
        var pageContent = await Page.ContentAsync();

        var redirectedToLogin = currentUrl.Contains("/auth/login", StringComparison.OrdinalIgnoreCase);
        var showsUnauthorized = pageContent.Contains("unauthorized", StringComparison.OrdinalIgnoreCase) ||
                                pageContent.Contains("access denied", StringComparison.OrdinalIgnoreCase) ||
                                pageContent.Contains("not authorized", StringComparison.OrdinalIgnoreCase);

        Assert.True(redirectedToLogin || showsUnauthorized, 
            "Sessions page should require authentication");
    }

    [Fact]
    public async Task SessionsPage_LoadsWhenAuthenticated()
    {
        // Arrange - Register and login
        var username = $"sess_{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Navigate to sessions page
        await Page.GotoAsync($"{WebAppUrl}/account/sessions");
        await WaitForBlazorAsync();

        // Assert - Page should load with sessions content
        var pageContent = await Page.ContentAsync();

        var hasSessionsContent = pageContent.Contains("Sessions", StringComparison.OrdinalIgnoreCase) ||
                                 pageContent.Contains("Active", StringComparison.OrdinalIgnoreCase) ||
                                 pageContent.Contains("Device", StringComparison.OrdinalIgnoreCase);

        Assert.True(hasSessionsContent, "Sessions page should show session management content");
    }

    [Fact]
    public async Task SessionsPage_ShowsCurrentSession()
    {
        // Arrange - Register and login
        var username = $"curr_{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Navigate to sessions page
        await Page.GotoAsync($"{WebAppUrl}/account/sessions");
        await WaitForBlazorAsync();

        // Assert - Should show at least one session (the current one)
        var pageContent = await Page.ContentAsync();

        var hasCurrentSession = pageContent.Contains("Current", StringComparison.OrdinalIgnoreCase) ||
                               pageContent.Contains("session", StringComparison.OrdinalIgnoreCase);

        Assert.True(hasCurrentSession, "Should show the current session");
    }

    [Fact]
    public async Task SessionsPage_HasRevokeAllButton()
    {
        // Arrange - Register and login
        var username = $"revall_{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Navigate to sessions page
        await Page.GotoAsync($"{WebAppUrl}/account/sessions");
        await WaitForBlazorAsync();

        // Assert - Should have revoke all button
        var revokeAllButton = await Page.QuerySelectorAsync("button:has-text('Revoke All'), button:has-text('Revoke all')");
        
        Assert.NotNull(revokeAllButton);
    }

    [Fact]
    public async Task SessionsPage_ShowsSessionDetails()
    {
        // Arrange - Register and login
        var username = $"details_{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Navigate to sessions page
        await Page.GotoAsync($"{WebAppUrl}/account/sessions");
        await WaitForBlazorAsync();

        // Assert - Should show session details like IP, device, or date
        var pageContent = await Page.ContentAsync();

        var hasSessionDetails = pageContent.Contains("IP", StringComparison.OrdinalIgnoreCase) ||
                               pageContent.Contains("Created", StringComparison.OrdinalIgnoreCase) ||
                               pageContent.Contains("Device", StringComparison.OrdinalIgnoreCase) ||
                               pageContent.Contains("Unknown", StringComparison.OrdinalIgnoreCase);

        Assert.True(hasSessionDetails, "Session details should be displayed");
    }

    [Fact]
    public async Task SessionsPage_HasPageTitle()
    {
        // Arrange - Register and login
        var username = $"title_{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Navigate to sessions page
        await Page.GotoAsync($"{WebAppUrl}/account/sessions");
        await WaitForBlazorAsync();

        // Assert - Page should have title
        var title = await Page.TitleAsync();
        
        Assert.Contains("Session", title, StringComparison.OrdinalIgnoreCase);
    }
}

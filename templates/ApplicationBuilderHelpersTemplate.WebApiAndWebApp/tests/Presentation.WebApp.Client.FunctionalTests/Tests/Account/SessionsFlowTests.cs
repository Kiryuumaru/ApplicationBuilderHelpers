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
        // Act
        await Page.GotoAsync($"{WebAppUrl}/account/sessions");
        await WaitForBlazorAsync();

        // Assert
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
        // Arrange
        var username = $"sess_{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act
        await Page.GotoAsync($"{WebAppUrl}/account/sessions");
        await WaitForBlazorAsync();

        // Assert
        var pageContent = await Page.ContentAsync();

        var hasSessionsContent = pageContent.Contains("Sessions", StringComparison.OrdinalIgnoreCase) ||
                                 pageContent.Contains("Active", StringComparison.OrdinalIgnoreCase) ||
                                 pageContent.Contains("Device", StringComparison.OrdinalIgnoreCase);

        Assert.True(hasSessionsContent, "Sessions page should show session management content");
    }

    [Fact]
    public async Task SessionsPage_ShowsCurrentSession()
    {
        // Arrange
        var username = $"curr_{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act
        await Page.GotoAsync($"{WebAppUrl}/account/sessions");
        await WaitForBlazorAsync();

        // Assert
        var pageContent = await Page.ContentAsync();

        var hasCurrentSession = pageContent.Contains("Current", StringComparison.OrdinalIgnoreCase) ||
                               pageContent.Contains("session", StringComparison.OrdinalIgnoreCase);

        Assert.True(hasCurrentSession, "Should show the current session");
    }

    [Fact]
    public async Task SessionsPage_HasRevokeAllButton()
    {
        // Arrange
        var username = $"revall_{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act
        await Page.GotoAsync($"{WebAppUrl}/account/sessions");
        await WaitForBlazorAsync();

        // Assert
        var revokeAllButton = await Page.QuerySelectorAsync("button:has-text('Revoke All'), button:has-text('Revoke all')");
        
        Assert.NotNull(revokeAllButton);
    }

    [Fact]
    public async Task SessionsPage_ShowsSessionDetails()
    {
        // Arrange
        var username = $"details_{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act
        await Page.GotoAsync($"{WebAppUrl}/account/sessions");
        await WaitForBlazorAsync();

        // Assert
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
        // Arrange
        var username = $"title_{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act
        await Page.GotoAsync($"{WebAppUrl}/account/sessions");
        await WaitForBlazorAsync();

        // Assert
        var title = await Page.TitleAsync();
        
        Assert.Contains("Session", title, StringComparison.OrdinalIgnoreCase);
    }
}

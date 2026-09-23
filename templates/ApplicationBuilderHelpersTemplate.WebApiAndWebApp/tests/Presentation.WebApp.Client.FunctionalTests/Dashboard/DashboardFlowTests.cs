using Presentation.WebApp.Client.FunctionalTests.Fixtures;

namespace Presentation.WebApp.Client.FunctionalTests.Dashboard;

/// <summary>
/// UI-only tests for dashboard/home page functionality.
/// All tests use mouse clicks and keyboard input only - like a real user.
/// </summary>
public class DashboardFlowTests : WebAppTestBase
{
    public DashboardFlowTests(PlaywrightFixture playwrightFixture, ITestOutputHelper output) : base(playwrightFixture, output)
    {
    }

    [Fact]
    public async Task Journey_DashboardLoads_HasContent()
    {
        // Act
        await GoToHomeAsync();

        // Assert
        var pageContent = await Page.ContentAsync();
        Assert.False(string.IsNullOrEmpty(pageContent));

    }

    [Fact]
    public async Task Journey_UnauthenticatedDashboard_ShowsGetStarted()
    {
        // Act
        await GoToHomeAsync();

        // Assert
        var pageContent = await Page.ContentAsync();
        var hasGetStarted = pageContent.Contains("get started", StringComparison.OrdinalIgnoreCase) ||
                           pageContent.Contains("sign in", StringComparison.OrdinalIgnoreCase) ||
                           pageContent.Contains("login", StringComparison.OrdinalIgnoreCase) ||
                           pageContent.Contains("register", StringComparison.OrdinalIgnoreCase);

        Assert.True(hasGetStarted, "Dashboard should prompt unauthenticated users");
    }

    [Fact]
    public async Task Journey_AuthenticatedDashboard_ShowsWelcome()
    {
        // Arrange
        var username = GenerateUsername("dash");
        var email = GenerateEmail(username);

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act
        await GoToHomeAsync();
        await WaitForBlazorAsync();

        // Assert
        var pageContent = await Page.ContentAsync();
        var hasWelcome = pageContent.Contains("welcome", StringComparison.OrdinalIgnoreCase) ||
                        pageContent.Contains("signed in", StringComparison.OrdinalIgnoreCase) ||
                        pageContent.Contains(username, StringComparison.OrdinalIgnoreCase) ||
                        pageContent.Contains("dashboard", StringComparison.OrdinalIgnoreCase);

        Assert.True(hasWelcome, "Authenticated dashboard should show welcome content");
    }

    [Fact]
    public async Task Journey_AuthenticatedDashboard_HasNavigationToProfile()
    {
        // Arrange
        var username = GenerateUsername("profnav");
        var email = GenerateEmail(username);

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act
        await GoToHomeAsync();
        await AssertIsAuthenticatedAsync();

        // Assert
        var userMenu = Page.Locator("button:has(.rounded-full)").First;
        Assert.True(await userMenu.CountAsync() > 0, "Should have user menu button");

        // Click to open menu
        await userMenu.ClickAsync();
        await Task.Delay(300);

        // Check for profile link
        var profileLink = Page.Locator("a[href*='profile']").First;
        Assert.True(await profileLink.CountAsync() > 0, "Should have profile link in menu");

    }

    [Fact]
    public async Task Journey_AuthenticatedDashboard_HasLogoutOption()
    {
        // Arrange
        var username = GenerateUsername("logout");
        var email = GenerateEmail(username);

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act
        await GoToHomeAsync();
        await AssertIsAuthenticatedAsync();

        // Open user menu
        var userMenu = Page.Locator("button:has(.rounded-full)").First;
        await userMenu.ClickAsync();
        await Task.Delay(300);

        // Assert
        var signOutButton = Page.Locator("button:has-text('Sign out')").First;
        Assert.True(await signOutButton.CountAsync() > 0, "Should have sign out button in menu");

    }

    [Fact]
    public async Task Journey_ClickLogoutFromDashboard_LogsOut()
    {
        // Arrange
        var username = GenerateUsername("logoutclick");
        var email = GenerateEmail(username);

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);
        await GoToHomeAsync();
        await AssertIsAuthenticatedAsync();

        // Act
        await LogoutAsync();

        // Assert
        await AssertIsNotAuthenticatedAsync();
        AssertUrlContains("/auth/login");

    }
}

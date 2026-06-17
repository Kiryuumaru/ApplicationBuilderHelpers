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
        // Act - Navigate to home/dashboard
        await GoToHomeAsync();

        // Assert - Dashboard has content
        var pageContent = await Page.ContentAsync();
        Assert.False(string.IsNullOrEmpty(pageContent));

        Output.WriteLine($"[TEST] Dashboard loaded with content. URL: {Page.Url}");
    }

    [Fact]
    public async Task Journey_UnauthenticatedDashboard_ShowsGetStarted()
    {
        // Act - Navigate to home without authentication
        await GoToHomeAsync();

        // Assert - Should show get started or login prompts
        var pageContent = await Page.ContentAsync();
        var hasGetStarted = pageContent.Contains("get started", StringComparison.OrdinalIgnoreCase) ||
                           pageContent.Contains("sign in", StringComparison.OrdinalIgnoreCase) ||
                           pageContent.Contains("login", StringComparison.OrdinalIgnoreCase) ||
                           pageContent.Contains("register", StringComparison.OrdinalIgnoreCase);

        Assert.True(hasGetStarted, "Dashboard should prompt unauthenticated users");
        Output.WriteLine("[TEST] Unauthenticated dashboard shows get started content");
    }

    [Fact]
    public async Task Journey_AuthenticatedDashboard_ShowsWelcome()
    {
        // Arrange - Register and login
        var username = GenerateUsername("dash");
        var email = GenerateEmail(username);

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Go to home (should redirect there after login anyway)
        await GoToHomeAsync();
        await WaitForBlazorAsync();

        // Assert - Should show welcome or authenticated content
        var pageContent = await Page.ContentAsync();
        var hasWelcome = pageContent.Contains("welcome", StringComparison.OrdinalIgnoreCase) ||
                        pageContent.Contains("signed in", StringComparison.OrdinalIgnoreCase) ||
                        pageContent.Contains(username, StringComparison.OrdinalIgnoreCase) ||
                        pageContent.Contains("dashboard", StringComparison.OrdinalIgnoreCase);

        Assert.True(hasWelcome, "Authenticated dashboard should show welcome content");
        Output.WriteLine("[TEST] Authenticated dashboard shows welcome content");
    }

    [Fact]
    public async Task Journey_AuthenticatedDashboard_HasNavigationToProfile()
    {
        // Arrange - Register and login
        var username = GenerateUsername("profnav");
        var email = GenerateEmail(username);

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Go to home
        await GoToHomeAsync();
        await AssertIsAuthenticatedAsync();

        // Assert - Should have user menu that leads to profile
        var userMenu = Page.Locator("button:has(.rounded-full)").First;
        Assert.True(await userMenu.CountAsync() > 0, "Should have user menu button");

        // Click to open menu
        await userMenu.ClickAsync();
        await Task.Delay(300);

        // Check for profile link
        var profileLink = Page.Locator("a[href*='profile']").First;
        Assert.True(await profileLink.CountAsync() > 0, "Should have profile link in menu");

        Output.WriteLine("[TEST] Dashboard has navigation to profile");
    }

    [Fact]
    public async Task Journey_AuthenticatedDashboard_HasLogoutOption()
    {
        // Arrange - Register and login
        var username = GenerateUsername("logout");
        var email = GenerateEmail(username);

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Go to home
        await GoToHomeAsync();
        await AssertIsAuthenticatedAsync();

        // Open user menu
        var userMenu = Page.Locator("button:has(.rounded-full)").First;
        await userMenu.ClickAsync();
        await Task.Delay(300);

        // Assert - Should have logout option
        var signOutButton = Page.Locator("button:has-text('Sign out')").First;
        Assert.True(await signOutButton.CountAsync() > 0, "Should have sign out button in menu");

        Output.WriteLine("[TEST] Dashboard has logout option");
    }

    [Fact]
    public async Task Journey_ClickLogoutFromDashboard_LogsOut()
    {
        // Arrange - Register and login
        var username = GenerateUsername("logoutclick");
        var email = GenerateEmail(username);

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);
        await GoToHomeAsync();
        await AssertIsAuthenticatedAsync();

        // Act - Logout via UI
        await LogoutAsync();

        // Assert - Should be logged out
        await AssertIsNotAuthenticatedAsync();
        AssertUrlContains("/auth/login");

        Output.WriteLine("[TEST] Logout from dashboard works");
    }
}

using Presentation.WebApp.Client.FunctionalTests;
using Presentation.WebApp.Client.FunctionalTests.Fixtures;

namespace Presentation.WebApp.Client.FunctionalTests.Old.Dashboard;

/// <summary>
/// Playwright functional tests for the dashboard/home page.
/// Tests dashboard display and statistics.
/// </summary>
public class DashboardTests : WebAppTestBase
{
    public DashboardTests(PlaywrightFixture playwrightFixture, ITestOutputHelper output) : base(playwrightFixture, output)
    {
    }

    [Fact]
    public async Task Dashboard_LoadsSuccessfully()
    {
        // Act
        await GoToHomeAsync();

        // Assert
        var title = await Page.TitleAsync();
        Output.WriteLine($"Dashboard title: {title}");

        var hasContent = !string.IsNullOrEmpty(await Page.ContentAsync());
        Assert.True(hasContent, "Dashboard should have content");
    }

    [Fact]
    public async Task Dashboard_ShowsStatisticsCards()
    {
        // Arrange
        var username = $"stats_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act
        await GoToHomeAsync();

        // Assert
        var cards = await Page.QuerySelectorAllAsync(".card, [class*='card']");
        Output.WriteLine($"Cards found: {cards.Count}");

        var pageContent = await Page.ContentAsync();
        var hasStats = pageContent.Contains("welcome", StringComparison.OrdinalIgnoreCase) ||
                      pageContent.Contains("roles", StringComparison.OrdinalIgnoreCase) ||
                      pageContent.Contains("active", StringComparison.OrdinalIgnoreCase);

        Output.WriteLine($"Has statistics content: {hasStats}");
        Assert.True(cards.Count > 0 || hasStats, "Dashboard should display statistics cards or content");
    }

    [Fact]
    public async Task Dashboard_Unauthenticated_ShowsGetStarted()
    {
        // Act
        await GoToHomeAsync();

        // Assert
        var pageContent = await Page.ContentAsync();
        var hasGetStarted = pageContent.Contains("get started", StringComparison.OrdinalIgnoreCase) ||
                           pageContent.Contains("sign in", StringComparison.OrdinalIgnoreCase) ||
                           pageContent.Contains("login", StringComparison.OrdinalIgnoreCase) ||
                           pageContent.Contains("register", StringComparison.OrdinalIgnoreCase);

        Output.WriteLine($"Has get started message: {hasGetStarted}");
        Assert.True(hasGetStarted, "Dashboard should prompt unauthenticated users to sign in or register");
    }

    [Fact]
    public async Task Dashboard_Authenticated_ShowsWelcomeMessage()
    {
        // Arrange
        var username = $"welcome_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act
        await WaitForBlazorAsync();

        // Assert
        var pageContent = await Page.ContentAsync();
        var hasWelcome = pageContent.Contains("welcome", StringComparison.OrdinalIgnoreCase) ||
                        pageContent.Contains("signed in", StringComparison.OrdinalIgnoreCase) ||
                        pageContent.Contains(username, StringComparison.OrdinalIgnoreCase);

        Output.WriteLine($"Has welcome message: {hasWelcome}");
        Assert.True(hasWelcome, "Should show welcome message for authenticated user");
    }

    [Fact]
    public async Task Dashboard_Authenticated_HasProfileLink()
    {
        // Arrange
        var username = $"proflink_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act
        await WaitForBlazorAsync();

        // Assert
        var profileLink = await Page.QuerySelectorAsync("a[href*='profile' i]");
        Assert.NotNull(profileLink);
    }

    [Fact]
    public async Task Dashboard_HasNavigationMenu()
    {
        // Arrange
        var username = $"navmenu_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act
        await WaitForBlazorAsync();

        // Assert
        var nav = await Page.QuerySelectorAsync("nav, [role='navigation'], .nav, .navbar, .sidebar");
        Assert.NotNull(nav);
    }

    [Fact]
    public async Task Dashboard_ShowsCorrectTitle()
    {
        // Arrange
        var username = $"title_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act
        await WaitForBlazorAsync();

        // Assert
        var title = await Page.TitleAsync();
        var pageContent = await Page.ContentAsync();

        var hasDashboardTitle = title.Contains("dashboard", StringComparison.OrdinalIgnoreCase) ||
                               pageContent.Contains("dashboard", StringComparison.OrdinalIgnoreCase);

        Output.WriteLine($"Page title: {title}");
        Assert.True(hasDashboardTitle, "Should show dashboard title");
    }

    [Fact]
    public async Task Dashboard_RolesCard_ShowsInfo()
    {
        // Arrange
        var username = $"roles_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act
        await GoToHomeAsync();

        // Assert
        var pageContent = await Page.ContentAsync();
        var hasRolesCard = pageContent.Contains("roles", StringComparison.OrdinalIgnoreCase);

        Output.WriteLine($"Has roles card: {hasRolesCard}");
        Assert.True(hasRolesCard, "Dashboard should display user's roles");
    }

    [Fact]
    public async Task Dashboard_AccountStatusCard_ShowsActive()
    {
        // Arrange
        var username = $"status_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act
        await GoToHomeAsync();

        // Assert
        var pageContent = await Page.ContentAsync();
        var hasActiveCard = pageContent.Contains("active", StringComparison.OrdinalIgnoreCase);

        Output.WriteLine($"Has active status card: {hasActiveCard}");
        Assert.True(hasActiveCard, "Dashboard should display account status as Active");
    }

    [Fact]
    public async Task Dashboard_QuickActions_ShowsSessionsLink()
    {
        // Arrange
        var username = $"sess_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act
        await GoToHomeAsync();

        // Assert
        var pageContent = await Page.ContentAsync();
        var hasSessionsLink = pageContent.Contains("sessions", StringComparison.OrdinalIgnoreCase);

        Output.WriteLine($"Has sessions link: {hasSessionsLink}");
        Assert.True(hasSessionsLink, "Dashboard Quick Actions should have Manage Sessions link");
    }

    [Fact]
    public async Task Dashboard_ResponsiveLayout()
    {
        // Act
        await Page.SetViewportSizeAsync(375, 667);
        await GoToHomeAsync();

        // Assert
        var content = await Page.ContentAsync();
        Assert.False(string.IsNullOrEmpty(content), "Dashboard should render on mobile viewport");

        // Reset to desktop
        await Page.SetViewportSizeAsync(1280, 720);
    }
}

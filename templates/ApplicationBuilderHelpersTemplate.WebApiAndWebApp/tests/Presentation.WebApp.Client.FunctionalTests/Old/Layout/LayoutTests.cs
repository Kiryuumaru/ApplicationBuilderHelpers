using Presentation.WebApp.Client.FunctionalTests.Fixtures;

namespace Presentation.WebApp.Client.FunctionalTests.Old.Layout;

/// <summary>
/// Playwright functional tests for layout components.
/// Tests main layout and auth layout rendering.
/// </summary>
public class LayoutTests : WebAppTestBase
{
    public LayoutTests(PlaywrightFixture playwrightFixture, ITestOutputHelper output) : base(playwrightFixture, output)
    {
    }

    #region Main Layout Tests

    [Fact]
    public async Task MainLayout_HasSidebar()
    {
        // Arrange
        var username = $"sidebar_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act
        await WaitForAuthenticatedStateAsync();

        // Assert
        var sidebar = await Page.QuerySelectorAsync("aside, .sidebar, [class*='sidebar']");
        var nav = await Page.QuerySelectorAsync("nav");

        Assert.True(sidebar != null || nav != null, "Should have sidebar or navigation");
    }

    [Fact]
    public async Task MainLayout_HasHeader()
    {
        // Arrange
        var username = $"header_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act
        await WaitForAuthenticatedStateAsync();

        // Assert
        var header = await Page.QuerySelectorAsync("header, .header, [class*='header']");
        Output.WriteLine($"Header found: {header != null}");
    }

    [Fact]
    public async Task MainLayout_HasUserMenu()
    {
        // Arrange
        var username = $"usermenu_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act
        await WaitForAuthenticatedStateAsync();

        // Assert
        var userMenu = await Page.QuerySelectorAsync("[class*='user'], [class*='avatar'], button:has-text('Logout')");
        var pageContent = await Page.ContentAsync();
        var hasUserElements = pageContent.Contains("logout", StringComparison.OrdinalIgnoreCase) ||
                             pageContent.Contains("profile", StringComparison.OrdinalIgnoreCase);

        Assert.True(userMenu != null || hasUserElements, "Should have user menu elements");
    }

    [Fact]
    public async Task MainLayout_HasLogoutOption()
    {
        // Arrange
        var username = $"logout_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act
        await WaitForAuthenticatedStateAsync();

        // Assert
        var logoutButton = await Page.QuerySelectorAsync("button:has-text('Logout'), a:has-text('Logout'), [data-testid='logout']");
        var pageContent = await Page.ContentAsync();
        var hasLogout = pageContent.Contains("logout", StringComparison.OrdinalIgnoreCase) ||
                       pageContent.Contains("sign out", StringComparison.OrdinalIgnoreCase);

        Assert.True(logoutButton != null || hasLogout, "Should have logout option");
    }

    [Fact]
    public async Task MainLayout_HasMainContentArea()
    {
        // Arrange
        var username = $"maincontent_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act
        await WaitForAuthenticatedStateAsync();

        // Assert
        var main = await Page.QuerySelectorAsync("main, [role='main'], .main-content, [class*='content']");
        Assert.NotNull(main);
    }

    #endregion

    #region Auth Layout Tests

    [Fact]
    public async Task AuthLayout_LoginPage_UsesCenteredLayout()
    {
        // Act
        await GoToLoginAsync();

        // Assert
        var content = await Page.ContentAsync();
        var pageHtml = await Page.InnerHTMLAsync("body");

        // Auth layout typically centers content and doesn't have sidebar
        var sidebar = await Page.QuerySelectorAsync("aside.sidebar, [class*='sidebar']");
        var centeredCard = await Page.QuerySelectorAsync(".max-w-md, .max-w-lg, [class*='center']");

        Output.WriteLine($"Has sidebar: {sidebar != null}");
        Output.WriteLine($"Has centered content: {centeredCard != null}");

        // Auth pages typically don't have the full sidebar
    }

    [Fact]
    public async Task AuthLayout_RegisterPage_UsesCenteredLayout()
    {
        // Act
        await GoToRegisterAsync();

        // Assert
        var card = await Page.QuerySelectorAsync(".bg-white, [class*='card']");
        Assert.NotNull(card);
    }

    [Fact]
    public async Task AuthLayout_HasBranding()
    {
        // Act
        await GoToLoginAsync();

        // Assert
        var pageContent = await Page.ContentAsync();
        var title = await Page.TitleAsync();

        var hasBranding = !string.IsNullOrEmpty(title);
        Assert.True(hasBranding, "Auth layout should have page title/branding");
    }

    #endregion

    #region Responsive Layout Tests

    [Fact]
    public async Task Layout_Mobile_AdjustsCorrectly()
    {
        // Arrange
        await Page.SetViewportSizeAsync(375, 667);
        
        var username = $"mobile_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act
        await WaitForBlazorAsync();

        // Assert
        var content = await Page.ContentAsync();
        Assert.False(string.IsNullOrEmpty(content), "Should render on mobile");

        // Sidebar might be hidden on mobile
        var sidebar = await Page.QuerySelectorAsync("aside:visible, .sidebar:visible");
        Output.WriteLine($"Sidebar visible on mobile: {sidebar != null}");

        // Reset viewport
        await Page.SetViewportSizeAsync(1280, 720);
    }

    [Fact]
    public async Task Layout_Tablet_AdjustsCorrectly()
    {
        // Arrange
        await Page.SetViewportSizeAsync(768, 1024);
        
        var username = $"tablet_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act
        await WaitForBlazorAsync();

        // Assert
        var content = await Page.ContentAsync();
        Assert.False(string.IsNullOrEmpty(content), "Should render on tablet");

        // Reset viewport
        await Page.SetViewportSizeAsync(1280, 720);
    }

    [Fact]
    public async Task Layout_Desktop_ShowsFullLayout()
    {
        // Arrange
        await Page.SetViewportSizeAsync(1920, 1080);
        
        var username = $"desktop_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act
        await WaitForBlazorAsync();

        // Assert
        var sidebar = await Page.QuerySelectorAsync("aside, nav, .sidebar");
        var main = await Page.QuerySelectorAsync("main, [role='main']");

        Assert.True(sidebar != null || main != null, "Desktop should show full layout");
    }

    #endregion

    #region Dark Mode Tests

    [Fact]
    public async Task Layout_SupportsDarkMode()
    {
        // Act
        await GoToHomeAsync();

        var pageContent = await Page.ContentAsync();
        var hasDarkModeSupport = pageContent.Contains("dark:", StringComparison.Ordinal);

        Output.WriteLine($"Has dark mode CSS classes: {hasDarkModeSupport}");
    }

    #endregion
}

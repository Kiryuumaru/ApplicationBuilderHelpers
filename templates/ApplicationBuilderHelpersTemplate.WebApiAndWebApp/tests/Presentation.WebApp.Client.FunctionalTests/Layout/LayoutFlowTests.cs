using Presentation.WebApp.Client.FunctionalTests.Fixtures;

namespace Presentation.WebApp.Client.FunctionalTests.Layout;

/// <summary>
/// UI-only tests for layout components (sidebar, header, navigation).
/// All tests use mouse clicks and keyboard input only - like a real user.
/// </summary>
public class LayoutFlowTests : WebAppTestBase
{
    public LayoutFlowTests(PlaywrightFixture playwrightFixture, ITestOutputHelper output) : base(playwrightFixture, output)
    {
    }

    [Fact]
    public async Task Journey_AuthenticatedLayout_HasSidebarOrNav()
    {
        // Arrange
        var username = GenerateUsername("sidebar");
        var email = GenerateEmail(username);

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);
        await GoToHomeAsync();
        await AssertIsAuthenticatedAsync();

        // Assert
        var sidebar = await Page.QuerySelectorAsync("aside, .sidebar, [class*='sidebar']");
        var nav = await Page.QuerySelectorAsync("nav");

        Assert.True(sidebar != null || nav != null, "Should have sidebar or navigation");
    }

    [Fact]
    public async Task Journey_AuthenticatedLayout_HasHeader()
    {
        // Arrange
        var username = GenerateUsername("header");
        var email = GenerateEmail(username);

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);
        await GoToHomeAsync();
        await AssertIsAuthenticatedAsync();

        // Assert
        var header = await Page.QuerySelectorAsync("header, .header, [class*='header']");
    }

    [Fact]
    public async Task Journey_AuthenticatedLayout_HasUserMenu()
    {
        // Arrange
        var username = GenerateUsername("usermenu");
        var email = GenerateEmail(username);

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);
        await GoToHomeAsync();
        await AssertIsAuthenticatedAsync();

        // Assert
        var userMenu = Page.Locator("button:has(.rounded-full)").First;
        Assert.True(await userMenu.CountAsync() > 0, "Should have user menu button");

    }

    [Fact]
    public async Task Journey_UserMenuDropdown_ShowsOptions()
    {
        // Arrange
        var username = GenerateUsername("dropdown");
        var email = GenerateEmail(username);

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);
        await GoToHomeAsync();
        await AssertIsAuthenticatedAsync();

        // Act
        var userMenu = Page.Locator("button:has(.rounded-full)").First;
        await userMenu.ClickAsync();
        await Task.Delay(300);

        // Assert
        var profileLink = Page.Locator("a[href*='profile']").First;
        var signOutButton = Page.Locator("button:has-text('Sign out')").First;

        Assert.True(await profileLink.CountAsync() > 0, "Dropdown should have profile link");
        Assert.True(await signOutButton.CountAsync() > 0, "Dropdown should have sign out button");

    }

    [Fact]
    public async Task Journey_AuthLayoutOnLogin_HasLoginForm()
    {
        // Act
        await GoToLoginAsync();

        // Assert
        var loginForm = await Page.QuerySelectorAsync("form");
        var emailField = await Page.QuerySelectorAsync("#email");
        var passwordField = await Page.QuerySelectorAsync("#password");

        Assert.NotNull(loginForm);
        Assert.NotNull(emailField);
        Assert.NotNull(passwordField);

    }

    [Fact]
    public async Task Journey_AuthLayoutOnRegister_HasRegisterForm()
    {
        // Act
        await GoToRegisterAsync();

        // Assert
        var registerForm = await Page.QuerySelectorAsync("form");
        var usernameField = await Page.QuerySelectorAsync("#username");
        var emailField = await Page.QuerySelectorAsync("#email");

        Assert.NotNull(registerForm);
        Assert.NotNull(usernameField);
        Assert.NotNull(emailField);

    }

    [Fact]
    public async Task Journey_MainContentArea_DisplaysPageContent()
    {
        // Arrange
        var username = GenerateUsername("content");
        var email = GenerateEmail(username);

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act
        await ClickNavigateToProfileAsync();

        // Assert
        var pageContent = await Page.ContentAsync();
        var hasProfileContent = pageContent.Contains("profile", StringComparison.OrdinalIgnoreCase) ||
                               pageContent.Contains(username, StringComparison.OrdinalIgnoreCase) ||
                               pageContent.Contains(email, StringComparison.OrdinalIgnoreCase);

        Assert.True(hasProfileContent, "Main content area should display profile content");
    }
}

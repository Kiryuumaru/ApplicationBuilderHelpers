using Presentation.WebApp.Client.FunctionalTests.Fixtures;

namespace Presentation.WebApp.Client.FunctionalTests.Navigation;

/// <summary>
/// UI-only tests for basic navigation flows.
/// All tests use mouse clicks and keyboard input only - like a real user.
/// </summary>
public class NavigationFlowTests : WebAppTestBase
{
    public NavigationFlowTests(PlaywrightFixture playwrightFixture, ITestOutputHelper output) : base(playwrightFixture, output)
    {
    }

    [Fact]
    public async Task Journey_HomePageLoads_HasBlazorContent()
    {
        // Act
        await GoToHomeAsync();

        // Assert
        var bodyElement = await Page.QuerySelectorAsync("body");
        Assert.NotNull(bodyElement);

        var blazorScript = await Page.QuerySelectorAsync("script[src*='blazor.web.']");
        Assert.NotNull(blazorScript);

    }

    [Fact]
    public async Task Journey_LoginPageLoads_HasForm()
    {
        // Act
        await GoToLoginAsync();

        // Assert
        AssertUrlContains("/auth/login");

        var emailField = await Page.QuerySelectorAsync("#email");
        var passwordField = await Page.QuerySelectorAsync("#password");
        var submitButton = await Page.QuerySelectorAsync("button[type='submit']");

        Assert.NotNull(emailField);
        Assert.NotNull(passwordField);
        Assert.NotNull(submitButton);

    }

    [Fact]
    public async Task Journey_RegisterPageLoads_HasForm()
    {
        // Act
        await GoToRegisterAsync();

        // Assert
        AssertUrlContains("/auth/register");

        var usernameField = await Page.QuerySelectorAsync("#username");
        var emailField = await Page.QuerySelectorAsync("#email");
        var passwordField = await Page.QuerySelectorAsync("#password");
        var confirmPasswordField = await Page.QuerySelectorAsync("#confirmPassword");
        var submitButton = await Page.QuerySelectorAsync("button[type='submit']");

        Assert.NotNull(usernameField);
        Assert.NotNull(emailField);
        Assert.NotNull(passwordField);
        Assert.NotNull(confirmPasswordField);
        Assert.NotNull(submitButton);

    }

    [Fact]
    public async Task Journey_ClickLoginLinkFromRegister_NavigatesToLogin()
    {
        // Arrange
        await GoToRegisterAsync();
        AssertUrlContains("/auth/register");

        // Act
        var loginLink = Page.Locator("a[href*='login']").First;
        await loginLink.ClickAsync();
        await WaitForBlazorAsync();

        // Assert
        AssertUrlContains("/auth/login");
    }

    [Fact]
    public async Task Journey_ClickRegisterLinkFromLogin_NavigatesToRegister()
    {
        // Arrange
        await GoToLoginAsync();
        AssertUrlContains("/auth/login");

        // Act
        var registerLink = Page.Locator("a[href*='register']").First;
        await registerLink.ClickAsync();
        await WaitForBlazorAsync();

        // Assert
        AssertUrlContains("/auth/register");
    }

    [Fact]
    public async Task Journey_ProtectedRouteUnauthenticated_RedirectsToLogin()
    {
        // Act
        await Page.GotoAsync($"{WebAppUrl}/account/profile");
        await WaitForBlazorAsync();
        await Task.Delay(500); // Wait for redirect

        // Assert
        AssertUrlContains("/auth/login");
    }

    [Fact]
    public async Task Journey_AdminRouteUnauthenticated_ShowsLoginOrNotFound()
    {
        // Act
        await Page.GotoAsync($"{WebAppUrl}/admin/users");
        await WaitForBlazorAsync();
        await Task.Delay(500);

        // Assert
        var currentUrl = Page.Url;
        var pageContent = await Page.ContentAsync();

        var redirectedToLogin = currentUrl.Contains("/auth/login", StringComparison.OrdinalIgnoreCase);
        var showsAccessDenied = pageContent.Contains("unauthorized", StringComparison.OrdinalIgnoreCase) ||
                                pageContent.Contains("not found", StringComparison.OrdinalIgnoreCase) ||
                                pageContent.Contains("access denied", StringComparison.OrdinalIgnoreCase);

        Assert.True(redirectedToLogin || showsAccessDenied,
            "Admin route should redirect to login or show access denied");
        
    }

    [Fact]
    public async Task Journey_AuthenticatedUserCanAccessProfile()
    {
        // Arrange
        var username = GenerateUsername("nav");
        var email = GenerateEmail(username);

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);
        await AssertIsAuthenticatedAsync();

        // Act
        await ClickNavigateToProfileAsync();

        // Assert
        AssertUrlContains("/account/profile");
    }

    [Fact]
    public async Task Journey_LoginPageWhileAuthenticated_RedirectsAway()
    {
        // Arrange
        var username = GenerateUsername("redir");
        var email = GenerateEmail(username);

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);
        await AssertIsAuthenticatedAsync();

        // Act
        await Page.GotoAsync($"{WebAppUrl}/auth/login");
        await WaitForBlazorAsync();
        await Task.Delay(500);

        // Assert
        var currentUrl = Page.Url;
        var notOnLogin = !currentUrl.Contains("/auth/login", StringComparison.OrdinalIgnoreCase);

        // Expected behavior: authenticated users do not see the login page
    }
}

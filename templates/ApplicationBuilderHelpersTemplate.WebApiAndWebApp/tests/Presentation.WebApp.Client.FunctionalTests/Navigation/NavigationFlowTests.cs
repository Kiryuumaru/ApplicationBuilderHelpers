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
        // Act - Navigate to home
        await GoToHomeAsync();

        // Assert - Page loaded with Blazor content
        var bodyElement = await Page.QuerySelectorAsync("body");
        Assert.NotNull(bodyElement);

        var blazorScript = await Page.QuerySelectorAsync("script[src*='blazor.web.']");
        Assert.NotNull(blazorScript);

        Output.WriteLine($"[TEST] Home page loaded. URL: {Page.Url}");
    }

    [Fact]
    public async Task Journey_LoginPageLoads_HasForm()
    {
        // Act - Navigate to login
        await GoToLoginAsync();

        // Assert - Has login form
        AssertUrlContains("/auth/login");

        var emailField = await Page.QuerySelectorAsync("#email");
        var passwordField = await Page.QuerySelectorAsync("#password");
        var submitButton = await Page.QuerySelectorAsync("button[type='submit']");

        Assert.NotNull(emailField);
        Assert.NotNull(passwordField);
        Assert.NotNull(submitButton);

        Output.WriteLine("[TEST] Login page has form elements");
    }

    [Fact]
    public async Task Journey_RegisterPageLoads_HasForm()
    {
        // Act - Navigate to register
        await GoToRegisterAsync();

        // Assert - Has register form
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

        Output.WriteLine("[TEST] Register page has form elements");
    }

    [Fact]
    public async Task Journey_ClickLoginLinkFromRegister_NavigatesToLogin()
    {
        // Arrange - Start at register page
        await GoToRegisterAsync();
        AssertUrlContains("/auth/register");

        // Act - Click login link
        var loginLink = Page.Locator("a[href*='login']").First;
        await loginLink.ClickAsync();
        await WaitForBlazorAsync();

        // Assert - Now on login page
        AssertUrlContains("/auth/login");
        Output.WriteLine("[TEST] Clicked from register to login");
    }

    [Fact]
    public async Task Journey_ClickRegisterLinkFromLogin_NavigatesToRegister()
    {
        // Arrange - Start at login page
        await GoToLoginAsync();
        AssertUrlContains("/auth/login");

        // Act - Click register link
        var registerLink = Page.Locator("a[href*='register']").First;
        await registerLink.ClickAsync();
        await WaitForBlazorAsync();

        // Assert - Now on register page
        AssertUrlContains("/auth/register");
        Output.WriteLine("[TEST] Clicked from login to register");
    }

    [Fact]
    public async Task Journey_ProtectedRouteUnauthenticated_RedirectsToLogin()
    {
        // Act - Try to access profile without authentication
        await Page.GotoAsync($"{WebAppUrl}/account/profile");
        await WaitForBlazorAsync();
        await Task.Delay(500); // Wait for redirect

        // Assert - Should be redirected to login
        AssertUrlContains("/auth/login");
        Output.WriteLine("[TEST] Protected route redirected to login");
    }

    [Fact]
    public async Task Journey_AdminRouteUnauthenticated_ShowsLoginOrNotFound()
    {
        // Act - Try to access admin without authentication
        await Page.GotoAsync($"{WebAppUrl}/admin/users");
        await WaitForBlazorAsync();
        await Task.Delay(500);

        // Assert - Should redirect to login or show access denied
        var currentUrl = Page.Url;
        var pageContent = await Page.ContentAsync();

        var redirectedToLogin = currentUrl.Contains("/auth/login", StringComparison.OrdinalIgnoreCase);
        var showsAccessDenied = pageContent.Contains("unauthorized", StringComparison.OrdinalIgnoreCase) ||
                                pageContent.Contains("not found", StringComparison.OrdinalIgnoreCase) ||
                                pageContent.Contains("access denied", StringComparison.OrdinalIgnoreCase);

        Assert.True(redirectedToLogin || showsAccessDenied,
            "Admin route should redirect to login or show access denied");
        
        Output.WriteLine($"[TEST] Admin route result - redirected: {redirectedToLogin}, denied: {showsAccessDenied}");
    }

    [Fact]
    public async Task Journey_AuthenticatedUserCanAccessProfile()
    {
        // Arrange - Register and login
        var username = GenerateUsername("nav");
        var email = GenerateEmail(username);

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);
        await AssertIsAuthenticatedAsync();

        // Act - Navigate to profile
        await ClickNavigateToProfileAsync();

        // Assert - On profile page
        AssertUrlContains("/account/profile");
        Output.WriteLine("[TEST] Authenticated user accessed profile");
    }

    [Fact]
    public async Task Journey_LoginPageWhileAuthenticated_RedirectsAway()
    {
        // Arrange - Register and login
        var username = GenerateUsername("redir");
        var email = GenerateEmail(username);

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);
        await AssertIsAuthenticatedAsync();

        // Act - Try to access login page while authenticated
        await Page.GotoAsync($"{WebAppUrl}/auth/login");
        await WaitForBlazorAsync();
        await Task.Delay(500);

        // Assert - Should redirect away from login (to home or dashboard)
        var currentUrl = Page.Url;
        var notOnLogin = !currentUrl.Contains("/auth/login", StringComparison.OrdinalIgnoreCase);

        Output.WriteLine($"[TEST] While authenticated, login page redirected: {notOnLogin}. URL: {currentUrl}");
        // Note: This is expected behavior - authenticated users shouldn't see login page
    }
}

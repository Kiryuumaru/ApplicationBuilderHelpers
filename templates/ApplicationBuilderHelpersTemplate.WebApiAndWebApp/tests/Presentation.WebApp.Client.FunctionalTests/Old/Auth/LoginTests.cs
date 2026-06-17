using Presentation.WebApp.Client.FunctionalTests;
using Presentation.WebApp.Client.FunctionalTests.Fixtures;

namespace Presentation.WebApp.Client.FunctionalTests.Old.Auth;

/// <summary>
/// Playwright functional tests for user login flow.
/// Tests the end-to-end login experience through the Blazor WebApp.
/// </summary>
public class LoginTests : WebAppTestBase
{
    public LoginTests(PlaywrightFixture playwrightFixture, ITestOutputHelper output) : base(playwrightFixture, output)
    {
    }

    [Fact]
    public async Task Login_PageLoads_ShowsLoginForm()
    {
        // Act
        await GoToLoginAsync();

        // Assert - Verify form elements exist (page uses email, not username)
        var emailInput = await Page.QuerySelectorAsync("input#email, input[type='email']");
        var passwordInput = await Page.QuerySelectorAsync("input#password, input[type='password']");
        var submitButton = await Page.QuerySelectorAsync("button[type='submit']");

        Assert.NotNull(emailInput);
        Assert.NotNull(passwordInput);
        Assert.NotNull(submitButton);
    }

    [Fact]
    public async Task Login_HasLinkToRegister()
    {
        // Act
        await GoToLoginAsync();

        // Assert - Should have a link to registration page
        var registerLink = await Page.QuerySelectorAsync("a[href*='register' i]");
        Assert.NotNull(registerLink);
    }

    [Fact]
    public async Task Login_WithValidCredentials_RedirectsToHome()
    {
        // Arrange - First register a new user
        var username = $"login_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);

        // Act - Now login (using email, not username)
        var success = await LoginAsync(email, TestPassword);

        // Assert
        Assert.True(success, "Login should succeed with valid credentials");
        AssertUrlDoesNotContain("/auth/login");
    }

    [Fact]
    public async Task Login_WithInvalidPassword_StaysOnLoginPage()
    {
        // Arrange - First register a new user
        var username = $"login_bad_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);

        // Act - Try to login with wrong password (using email, not username)
        await GoToLoginAsync();
        await Page.FillAsync("input#email, input[type='email']", email);
        await Page.FillAsync("input#password, input[type='password']", "WrongPassword123!");
        await Page.ClickAsync("button[type='submit']");

        await Task.Delay(1000);

        // Assert - Should still be on login page
        var currentUrl = Page.Url;
        Output.WriteLine($"URL after bad password: {currentUrl}");

        var hasError = await Page.QuerySelectorAsync(".error, .validation-error, [class*='error'], .text-red");
        var stillOnLogin = currentUrl.Contains("/auth/login", StringComparison.OrdinalIgnoreCase);

        Assert.True(hasError != null || stillOnLogin, "Should show error or stay on login page");
    }

    [Fact]
    public async Task Login_WithNonExistentUser_StaysOnLoginPage()
    {
        // Arrange
        var email = $"nonexistent_{Guid.NewGuid():N}@test.example.com";

        // Act (using email, not username)
        await GoToLoginAsync();
        await Page.FillAsync("input#email, input[type='email']", email);
        await Page.FillAsync("input#password, input[type='password']", TestPassword);
        await Page.ClickAsync("button[type='submit']");

        await Task.Delay(1000);

        // Assert - Should still be on login page
        var currentUrl = Page.Url;
        Output.WriteLine($"URL after non-existent user: {currentUrl}");

        var hasError = await Page.QuerySelectorAsync(".error, .validation-error, [class*='error'], .text-red");
        var stillOnLogin = currentUrl.Contains("/auth/login", StringComparison.OrdinalIgnoreCase);

        Assert.True(hasError != null || stillOnLogin, "Should show error or stay on login page");
    }

    [Fact]
    public async Task Login_ClickRegisterLink_NavigatesToRegister()
    {
        // Arrange
        await GoToLoginAsync();

        // Act - Click register link
        await Page.ClickAsync("a[href*='register' i]");
        await WaitForBlazorAsync();

        // Assert
        AssertUrlContains("/auth/register");
    }
}

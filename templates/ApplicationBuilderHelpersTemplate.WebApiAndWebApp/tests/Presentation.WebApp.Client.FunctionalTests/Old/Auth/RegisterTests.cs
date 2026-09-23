using Presentation.WebApp.Client.FunctionalTests.Fixtures;

namespace Presentation.WebApp.Client.FunctionalTests.Old.Auth;

/// <summary>
/// Playwright functional tests for user registration flow.
/// Tests the end-to-end registration experience through the Blazor WebApp.
/// </summary>
public class RegisterTests : WebAppTestBase
{
    public RegisterTests(PlaywrightFixture playwrightFixture, ITestOutputHelper output) : base(playwrightFixture, output)
    {
    }

    [Fact]
    public async Task Register_NewUser_RedirectsToLoginOrHome()
    {
        // Arrange
        var username = $"reg_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        // Act
        var success = await RegisterUserAsync(username, email, TestPassword);

        // Assert
        Assert.True(success, "Registration should succeed for a new user");
    }

    [Fact]
    public async Task Register_PageLoads_ShowsRegistrationForm()
    {
        // Act
        await GoToRegisterAsync();

        // Assert
        var usernameInput = await Page.QuerySelectorAsync("input#username");
        var emailInput = await Page.QuerySelectorAsync("input#email");
        var passwordInput = await Page.QuerySelectorAsync("input#password");
        var submitButton = await Page.QuerySelectorAsync("button[type='submit']");

        Assert.NotNull(usernameInput);
        Assert.NotNull(emailInput);
        Assert.NotNull(passwordInput);
        Assert.NotNull(submitButton);
    }

    [Fact]
    public async Task Register_HasLinkToLogin()
    {
        // Act
        await GoToRegisterAsync();

        // Assert
        var loginLink = await Page.QuerySelectorAsync("a[href*='login' i]");
        Assert.NotNull(loginLink);
    }

    [Fact]
    public async Task Register_WithWeakPassword_ShowsError()
    {
        // Arrange
        await GoToRegisterAsync();
        var username = $"reg_{Guid.NewGuid():N}".Substring(0, 20);

        // Act
        await Page.FillAsync("input#username", username);
        await Page.FillAsync("input#email", $"{username}@test.example.com");
        await Page.FillAsync("input#password", "weak");
        await Page.FillAsync("input#confirmPassword", "weak");

        await Page.ClickAsync("button[type='submit']");
        await Task.Delay(1000);

        // Assert
        // The page can also show a validation message depending on implementation
        var currentUrl = Page.Url;
        Output.WriteLine($"URL after weak password: {currentUrl}");

        // Either shows error message or stays on register page
        var hasError = await Page.QuerySelectorAsync(".error, .validation-error, [class*='error'], .text-red");
        var stillOnRegister = currentUrl.Contains("/auth/register", StringComparison.OrdinalIgnoreCase);

        Assert.True(hasError != null || stillOnRegister, "Should show error or stay on register page");
    }
}

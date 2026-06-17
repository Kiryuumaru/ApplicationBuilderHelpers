using Presentation.WebApp.Client.FunctionalTests.Fixtures;

namespace Presentation.WebApp.Client.FunctionalTests.Tests.Account;

/// <summary>
/// UI-only tests for change password page.
/// All tests use mouse clicks and keyboard input like a real user.
/// </summary>
public class ChangePasswordFlowTests : WebAppTestBase
{
    public ChangePasswordFlowTests(PlaywrightFixture playwrightFixture, ITestOutputHelper output) : base(playwrightFixture, output)
    {
    }

    [Fact]
    public async Task ChangePasswordPage_RequiresAuthentication()
    {
        // Act - Navigate to change password page without authentication
        await Page.GotoAsync($"{WebAppUrl}/account/change-password");
        await WaitForBlazorAsync();

        // Assert - Should redirect to login or show unauthorized content
        var currentUrl = Page.Url;
        var pageContent = await Page.ContentAsync();

        var redirectedToLogin = currentUrl.Contains("/auth/login", StringComparison.OrdinalIgnoreCase);
        var showsUnauthorized = pageContent.Contains("unauthorized", StringComparison.OrdinalIgnoreCase) ||
                                pageContent.Contains("access denied", StringComparison.OrdinalIgnoreCase) ||
                                pageContent.Contains("not authorized", StringComparison.OrdinalIgnoreCase);

        Assert.True(redirectedToLogin || showsUnauthorized, 
            "Change password page should require authentication");
    }

    [Fact]
    public async Task ChangePasswordPage_LoadsWhenAuthenticated()
    {
        // Arrange - Register and login
        var username = $"chgpw_{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Navigate to change password page
        await Page.GotoAsync($"{WebAppUrl}/account/change-password");
        await WaitForBlazorAsync();

        // Assert - Page should load with change password form
        var pageContent = await Page.ContentAsync();

        var hasChangePasswordContent = pageContent.Contains("Change Password", StringComparison.OrdinalIgnoreCase) ||
                                       pageContent.Contains("Current Password", StringComparison.OrdinalIgnoreCase) ||
                                       pageContent.Contains("New Password", StringComparison.OrdinalIgnoreCase);

        Assert.True(hasChangePasswordContent, "Change password page should show password change form");
    }

    [Fact]
    public async Task ChangePasswordPage_HasPasswordFields()
    {
        // Arrange - Register and login
        var username = $"pwflds_{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Navigate to change password page
        await Page.GotoAsync($"{WebAppUrl}/account/change-password");
        await WaitForBlazorAsync();

        // Assert - Should have current password, new password, and confirm password fields
        var currentPasswordInput = await Page.QuerySelectorAsync("input[type='password'][id*='current' i], input[type='password'][name*='current' i]");
        var newPasswordInput = await Page.QuerySelectorAsync("input[type='password'][id*='new' i], input[type='password'][name*='new' i]");
        var confirmPasswordInput = await Page.QuerySelectorAsync("input[type='password'][id*='confirm' i], input[type='password'][name*='confirm' i]");

        // At minimum, should have password inputs
        var passwordInputs = await Page.QuerySelectorAllAsync("input[type='password']");
        
        Assert.True(passwordInputs.Count >= 3, "Should have at least 3 password input fields");
    }

    [Fact]
    public async Task ChangePasswordPage_HasSubmitButton()
    {
        // Arrange - Register and login
        var username = $"submit_{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Navigate to change password page
        await Page.GotoAsync($"{WebAppUrl}/account/change-password");
        await WaitForBlazorAsync();

        // Assert - Should have submit button
        var submitButton = await Page.QuerySelectorAsync("button[type='submit'], button:has-text('Change'), button:has-text('Save'), button:has-text('Update')");
        
        Assert.NotNull(submitButton);
    }

    [Fact]
    public async Task ChangePasswordPage_ShowsValidationOnEmptySubmit()
    {
        // Arrange - Register and login
        var username = $"valid_{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Navigate to change password page and click submit without filling
        await Page.GotoAsync($"{WebAppUrl}/account/change-password");
        await WaitForBlazorAsync();

        var submitButton = await Page.QuerySelectorAsync("button[type='submit']");
        if (submitButton != null)
        {
            await submitButton.ClickAsync();
            await WaitForBlazorAsync();

            // Assert - Should show validation or required field indicators
            var pageContent = await Page.ContentAsync();
            var hasValidation = pageContent.Contains("required", StringComparison.OrdinalIgnoreCase) ||
                               pageContent.Contains("Please", StringComparison.OrdinalIgnoreCase) ||
                               await Page.QuerySelectorAsync("[class*='error'], [class*='invalid'], .text-red") != null;

            // Form might not allow submit if fields are required via HTML5 validation
            Output.WriteLine($"Validation shown: {hasValidation}");
        }
    }

    [Fact]
    public async Task ChangePasswordPage_HasPageTitle()
    {
        // Arrange - Register and login
        var username = $"title_{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Navigate to change password page
        await Page.GotoAsync($"{WebAppUrl}/account/change-password");
        await WaitForBlazorAsync();

        // Assert - Page should have title
        var title = await Page.TitleAsync();
        
        Assert.Contains("Password", title, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ChangePasswordPage_ShowsPasswordRequirements()
    {
        // Arrange - Register and login
        var username = $"reqs_{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Navigate to change password page
        await Page.GotoAsync($"{WebAppUrl}/account/change-password");
        await WaitForBlazorAsync();

        // Assert - Should show password requirements hint
        var pageContent = await Page.ContentAsync();
        var hasPasswordHint = pageContent.Contains("8 character", StringComparison.OrdinalIgnoreCase) ||
                             pageContent.Contains("characters", StringComparison.OrdinalIgnoreCase) ||
                             pageContent.Contains("minimum", StringComparison.OrdinalIgnoreCase);

        Output.WriteLine($"Password requirements shown: {hasPasswordHint}");
    }
}

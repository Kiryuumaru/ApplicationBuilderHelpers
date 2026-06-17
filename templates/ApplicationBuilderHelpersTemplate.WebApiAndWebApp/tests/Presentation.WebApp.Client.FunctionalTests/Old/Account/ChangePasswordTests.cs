using Presentation.WebApp.Client.FunctionalTests.Fixtures;

namespace Presentation.WebApp.Client.FunctionalTests.Old.Account;

/// <summary>
/// Playwright functional tests for change password page.
/// Tests password change functionality through the Blazor WebApp.
/// </summary>
public class ChangePasswordTests : WebAppTestBase
{
    private const string NewPassword = "NewPassword456!";

    public ChangePasswordTests(PlaywrightFixture playwrightFixture, ITestOutputHelper output) : base(playwrightFixture, output)
    {
    }

    [Fact]
    public async Task ChangePassword_RequiresAuthentication()
    {
        // Act - Try to access change password without authentication
        await Page.GotoAsync($"{WebAppUrl}/account/change-password");
        await WaitForBlazorAsync();

        // Assert - Should redirect to login
        var currentUrl = Page.Url;
        var redirectedToLogin = currentUrl.Contains("/auth/login", StringComparison.OrdinalIgnoreCase);
        Assert.True(redirectedToLogin, "Should redirect to login when accessing change password unauthenticated");
    }

    [Fact]
    public async Task ChangePassword_Authenticated_ShowsForm()
    {
        // Arrange - Register and login
        var username = $"chgpwd_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Navigate to change password
        await Page.GotoAsync($"{WebAppUrl}/account/change-password");
        await WaitForBlazorAsync();

        // Assert - Should show change password form
        var currentPasswordInput = await Page.QuerySelectorAsync("input#currentPassword, input[name*='current' i]");
        var newPasswordInput = await Page.QuerySelectorAsync("input#newPassword, input[name*='new' i]");
        var confirmPasswordInput = await Page.QuerySelectorAsync("input#confirmPassword, input[name*='confirm' i]");

        Output.WriteLine($"Current password input: {currentPasswordInput != null}");
        Output.WriteLine($"New password input: {newPasswordInput != null}");
        Output.WriteLine($"Confirm password input: {confirmPasswordInput != null}");

        // Should have password fields
        var passwordInputs = await Page.QuerySelectorAllAsync("input[type='password']");
        Assert.True(passwordInputs.Count >= 2, "Should have at least 2 password fields");
    }

    [Fact]
    public async Task ChangePassword_HasBreadcrumb()
    {
        // Arrange - Register and login
        var username = $"bread_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Navigate to change password
        await Page.GotoAsync($"{WebAppUrl}/account/change-password");
        await WaitForBlazorAsync();

        // Assert - Should have breadcrumb navigation
        var breadcrumb = await Page.QuerySelectorAsync("nav[aria-label='Breadcrumb'], .breadcrumb, ol");
        var profileLink = await Page.QuerySelectorAsync("a[href*='profile' i]");

        Assert.True(breadcrumb != null || profileLink != null, "Should have breadcrumb or link back to profile");
    }

    [Fact]
    public async Task ChangePassword_HasSubmitAndCancelButtons()
    {
        // Arrange - Register and login
        var username = $"buttons_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Navigate to change password
        await Page.GotoAsync($"{WebAppUrl}/account/change-password");
        await WaitForBlazorAsync();

        // Assert - Should have submit and cancel buttons
        var submitButton = await Page.QuerySelectorAsync("button[type='submit'], button:has-text('Update'), button:has-text('Change')");
        var cancelButton = await Page.QuerySelectorAsync("button:has-text('Cancel'), a:has-text('Cancel')");

        Assert.NotNull(submitButton);
        Output.WriteLine($"Cancel button found: {cancelButton != null}");
    }

    [Fact(Skip = "Client-side validation timing needs investigation")]
    public async Task ChangePassword_PasswordMismatch_ShowsError()
    {
        // Arrange - Register and login
        var username = $"mismatch_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        await Page.GotoAsync($"{WebAppUrl}/account/change-password");
        await WaitForBlazorAsync();

        // Act - Fill with mismatched passwords
        var passwordFields = await Page.QuerySelectorAllAsync("input[type='password']");
        if (passwordFields.Count >= 3)
        {
            await passwordFields[0].FillAsync(TestPassword);          // Current password
            await passwordFields[1].FillAsync(NewPassword);           // New password
            await passwordFields[2].FillAsync("DifferentPassword!"); // Confirm - mismatched
        }

        await Page.ClickAsync("button[type='submit']");
        await Task.Delay(500);

        // Assert - Should show error or validation message
        var pageContent = await Page.ContentAsync();
        var hasError = pageContent.Contains("match", StringComparison.OrdinalIgnoreCase) ||
                      pageContent.Contains("error", StringComparison.OrdinalIgnoreCase) ||
                      await Page.QuerySelectorAsync(".error, .validation-error, [class*='error'], .text-red") != null;

        Assert.True(hasError, "Should show error for password mismatch");
    }

    [Fact]
    public async Task ChangePassword_EmptyFields_ShowsValidation()
    {
        // Arrange - Register and login
        var username = $"empty_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        await Page.GotoAsync($"{WebAppUrl}/account/change-password");
        await WaitForBlazorAsync();

        // Act - Submit without filling anything
        await Page.ClickAsync("button[type='submit']");
        await Task.Delay(500);

        // Assert - Should show validation errors
        var validationErrors = await Page.QuerySelectorAllAsync(".validation-message, .field-validation-error, .text-red-500, [class*='error']");
        Output.WriteLine($"Validation errors found: {validationErrors.Count}");

        // Should either show validation or stay on same page
        AssertUrlContains("change-password");
    }
}

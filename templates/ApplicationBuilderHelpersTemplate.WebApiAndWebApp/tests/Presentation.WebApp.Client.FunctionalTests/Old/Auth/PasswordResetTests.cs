using Presentation.WebApp.Client.FunctionalTests.Fixtures;

namespace Presentation.WebApp.Client.FunctionalTests.Old.Auth;

/// <summary>
/// Playwright functional tests for forgot/reset password flow.
/// Tests password recovery functionality through the Blazor WebApp.
/// </summary>
public class PasswordResetTests : WebAppTestBase
{
    private const string NewPassword = "NewPassword456!";

    public PasswordResetTests(PlaywrightFixture playwrightFixture, ITestOutputHelper output) : base(playwrightFixture, output)
    {
    }

    [Fact]
    public async Task ForgotPassword_PageLoads_ShowsEmailForm()
    {
        // Act
        await Page.GotoAsync($"{WebAppUrl}/auth/forgot-password");
        await WaitForBlazorAsync();

        // Assert - Verify form elements exist
        var emailInput = await Page.QuerySelectorAsync("input[type='email'], input[name='email']");
        var submitButton = await Page.QuerySelectorAsync("button[type='submit']");

        Assert.NotNull(emailInput);
        Assert.NotNull(submitButton);
    }

    [Fact]
    public async Task ForgotPassword_HasBackToLoginLink()
    {
        // Act
        await Page.GotoAsync($"{WebAppUrl}/auth/forgot-password");
        await WaitForBlazorAsync();

        // Assert - Should have a link back to login
        var loginLink = await Page.QuerySelectorAsync("a[href*='login' i]");
        Assert.NotNull(loginLink);
    }

    [Fact]
    public async Task ForgotPassword_SubmitEmail_ShowsConfirmation()
    {
        // Arrange
        await Page.GotoAsync($"{WebAppUrl}/auth/forgot-password");
        await WaitForBlazorAsync();

        // Act - Submit email
        var emailInput = await Page.QuerySelectorAsync("input[type='email'], input[name='email']");
        if (emailInput != null)
        {
            await emailInput.FillAsync("test@example.com");
        }

        await Page.ClickAsync("button[type='submit']");
        await Task.Delay(1000);

        // Assert - Should show confirmation message after submitting email
        var pageContent = await Page.ContentAsync();
        Output.WriteLine($"Page content after submit: {pageContent.Substring(0, Math.Min(500, pageContent.Length))}");

        // Should show success message indicating email was sent
        var hasSuccessIndicator = pageContent.Contains("email", StringComparison.OrdinalIgnoreCase) &&
                                  (pageContent.Contains("sent", StringComparison.OrdinalIgnoreCase) ||
                                   pageContent.Contains("check", StringComparison.OrdinalIgnoreCase));

        Assert.True(hasSuccessIndicator, "Should show confirmation that password reset email was sent");
    }

    [Fact]
    public async Task Login_HasForgotPasswordLink()
    {
        // Act
        await GoToLoginAsync();

        // Assert - Should have forgot password link
        var forgotLink = await Page.QuerySelectorAsync("a[href*='forgot' i], a[href*='reset' i]");
        Assert.NotNull(forgotLink);
    }

    [Fact]
    public async Task Login_ClickForgotPassword_NavigatesToForgotPage()
    {
        // Arrange
        await GoToLoginAsync();

        // Act - Click forgot password link
        var forgotLink = await Page.QuerySelectorAsync("a[href*='forgot' i], a[href*='reset' i]");
        if (forgotLink != null)
        {
            await forgotLink.ClickAsync();
            await WaitForBlazorAsync();

            // Assert
            var currentUrl = Page.Url;
            Assert.True(
                currentUrl.Contains("forgot", StringComparison.OrdinalIgnoreCase) ||
                currentUrl.Contains("reset", StringComparison.OrdinalIgnoreCase),
                $"Should navigate to forgot/reset password page. Current: {currentUrl}");
        }
    }
}

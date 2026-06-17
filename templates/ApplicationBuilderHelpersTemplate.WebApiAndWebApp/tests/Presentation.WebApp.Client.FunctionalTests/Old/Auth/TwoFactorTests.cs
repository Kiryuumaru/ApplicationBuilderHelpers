using Presentation.WebApp.Client.FunctionalTests;
using Presentation.WebApp.Client.FunctionalTests.Fixtures;

namespace Presentation.WebApp.Client.FunctionalTests.Old.Auth;

/// <summary>
/// Playwright functional tests for two-factor authentication flow.
/// Tests 2FA setup and verification through the Blazor WebApp.
/// </summary>
public class TwoFactorTests : WebAppTestBase
{
    public TwoFactorTests(PlaywrightFixture playwrightFixture, ITestOutputHelper output) : base(playwrightFixture, output)
    {
    }

    [Fact]
    public async Task TwoFactorSetup_RequiresAuthentication()
    {
        // Act - Try to access 2FA setup without authentication
        await Page.GotoAsync($"{WebAppUrl}/account/two-factor");
        await WaitForBlazorAsync();

        // Assert - Should redirect to login
        var currentUrl = Page.Url;
        Output.WriteLine($"2FA setup URL when unauthenticated: {currentUrl}");

        var redirectedToLogin = currentUrl.Contains("/auth/login", StringComparison.OrdinalIgnoreCase);
        Assert.True(redirectedToLogin, "Should redirect to login when accessing 2FA setup unauthenticated");
    }

    [Fact]
    public async Task TwoFactorSetup_Authenticated_ShowsSetupPage()
    {
        // Arrange - Register and login
        var username = $"2fa_setup_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Navigate to 2FA setup
        await Page.GotoAsync($"{WebAppUrl}/account/two-factor");
        await WaitForBlazorAsync();

        // Assert - Should show 2FA setup page content
        var pageContent = await Page.ContentAsync();
        Output.WriteLine($"2FA setup page loaded");

        var has2FAContent = pageContent.Contains("two-factor", StringComparison.OrdinalIgnoreCase) ||
                           pageContent.Contains("2fa", StringComparison.OrdinalIgnoreCase) ||
                           pageContent.Contains("authenticator", StringComparison.OrdinalIgnoreCase) ||
                           pageContent.Contains("qr", StringComparison.OrdinalIgnoreCase);

        Assert.True(has2FAContent, "Should show 2FA setup content");
    }

    [Fact]
    public async Task TwoFactorSetup_ShowsQRCodePlaceholder()
    {
        // Arrange - Register and login
        var username = $"2fa_qr_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Navigate to 2FA setup
        await Page.GotoAsync($"{WebAppUrl}/account/two-factor");
        await WaitForBlazorAsync();

        // Assert - Should have QR code area or setup instructions
        var pageContent = await Page.ContentAsync();
        var hasSetupInstructions = pageContent.Contains("step", StringComparison.OrdinalIgnoreCase) ||
                                   pageContent.Contains("scan", StringComparison.OrdinalIgnoreCase) ||
                                   pageContent.Contains("authenticator", StringComparison.OrdinalIgnoreCase);

        Assert.True(hasSetupInstructions, "Should show 2FA setup instructions");
    }

    [Fact]
    public async Task TwoFactorSetup_HasSetupOrVerificationOption()
    {
        // Arrange - Register and login
        var username = $"2fa_input_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Navigate to 2FA setup
        await Page.GotoAsync($"{WebAppUrl}/account/two-factor");
        await WaitForBlazorAsync();

        // Assert - Should have setup button (initial state), code input (after starting setup), 
        // or enable/verify button
        var setupButton = await Page.QuerySelectorAsync("button:has-text('Set Up'), button:has-text('Setup')");
        var codeInput = await Page.QuerySelectorAsync("input[type='text'], input[placeholder*='000000'], input#code, input#verificationCode");
        var enableButton = await Page.QuerySelectorAsync("button:has-text('Enable'), button:has-text('Verify')");

        Output.WriteLine($"Setup button found: {setupButton != null}");
        Output.WriteLine($"Code input found: {codeInput != null}");
        Output.WriteLine($"Enable button found: {enableButton != null}");

        // At least one verification/setup element should exist
        Assert.True(setupButton != null || codeInput != null || enableButton != null, "Should have setup button, code input, or enable button");
    }

    [Fact]
    public async Task Profile_HasTwoFactorLink()
    {
        // Arrange - Register and login
        var username = $"2fa_link_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Navigate to profile
        await Page.GotoAsync($"{WebAppUrl}/account/profile");
        await WaitForBlazorAsync();

        // Assert - Should have link to 2FA setup
        var twoFactorLink = await Page.QuerySelectorAsync("a[href*='two-factor' i]");
        Assert.NotNull(twoFactorLink);
    }
}

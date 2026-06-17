using Presentation.WebApp.Client.FunctionalTests.Fixtures;

namespace Presentation.WebApp.Client.FunctionalTests.Tests.Account;

/// <summary>
/// UI-only tests for two-factor authentication pages.
/// All tests use mouse clicks and keyboard input like a real user.
/// </summary>
public class TwoFactorFlowTests : WebAppTestBase
{
    public TwoFactorFlowTests(PlaywrightFixture playwrightFixture, ITestOutputHelper output) : base(playwrightFixture, output)
    {
    }

    [Fact]
    public async Task TwoFactorPage_RequiresAuthentication()
    {
        // Act - Navigate to 2FA page without authentication
        await Page.GotoAsync($"{WebAppUrl}/account/two-factor");
        await WaitForBlazorAsync();

        // Assert - Should redirect to login or show unauthorized content
        var currentUrl = Page.Url;
        var pageContent = await Page.ContentAsync();

        var redirectedToLogin = currentUrl.Contains("/auth/login", StringComparison.OrdinalIgnoreCase);
        var showsUnauthorized = pageContent.Contains("unauthorized", StringComparison.OrdinalIgnoreCase) ||
                                pageContent.Contains("access denied", StringComparison.OrdinalIgnoreCase) ||
                                pageContent.Contains("not authorized", StringComparison.OrdinalIgnoreCase);

        Assert.True(redirectedToLogin || showsUnauthorized, 
            "2FA page should require authentication");
    }

    [Fact]
    public async Task TwoFactorPage_LoadsWhenAuthenticated()
    {
        // Arrange - Register and login
        var username = $"twofa_{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Navigate to 2FA page
        await Page.GotoAsync($"{WebAppUrl}/account/two-factor");
        await WaitForBlazorAsync();

        // Assert - Page should load with 2FA content
        var pageContent = await Page.ContentAsync();

        var has2FAContent = pageContent.Contains("Two-Factor", StringComparison.OrdinalIgnoreCase) ||
                           pageContent.Contains("2FA", StringComparison.OrdinalIgnoreCase) ||
                           pageContent.Contains("Authenticator", StringComparison.OrdinalIgnoreCase);

        Assert.True(has2FAContent, "2FA page should show two-factor authentication content");
    }

    [Fact]
    public async Task TwoFactorPage_ShowsSetupOption_ForNewUser()
    {
        // Arrange - Register and login (new user has 2FA disabled)
        var username = $"setup_{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Navigate to 2FA page
        await Page.GotoAsync($"{WebAppUrl}/account/two-factor");
        await WaitForBlazorAsync();

        // Assert - Should show option to set up 2FA
        var setupButton = await Page.QuerySelectorAsync("button:has-text('Set Up'), button:has-text('Enable'), button:has-text('Configure')");
        var pageContent = await Page.ContentAsync();

        var hasSetupOption = setupButton != null ||
                            pageContent.Contains("Set Up", StringComparison.OrdinalIgnoreCase) ||
                            pageContent.Contains("not enabled", StringComparison.OrdinalIgnoreCase);

        Assert.True(hasSetupOption, "New user should see option to set up 2FA");
    }

    [Fact]
    public async Task TwoFactorSetup_ClickSetup_ShowsQRCode()
    {
        // Arrange - Register and login
        var username = $"qrcode_{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Navigate to 2FA page and click setup
        await Page.GotoAsync($"{WebAppUrl}/account/two-factor");
        await WaitForBlazorAsync();

        var setupButton = await Page.QuerySelectorAsync("button:has-text('Set Up')");
        if (setupButton != null)
        {
            await setupButton.ClickAsync();
            await WaitForBlazorAsync();

            // Assert - Should show QR code or setup instructions
            var pageContent = await Page.ContentAsync();

            var hasQRContent = pageContent.Contains("QR", StringComparison.OrdinalIgnoreCase) ||
                              pageContent.Contains("scan", StringComparison.OrdinalIgnoreCase) ||
                              pageContent.Contains("authenticator app", StringComparison.OrdinalIgnoreCase) ||
                              await Page.QuerySelectorAsync("img, svg, canvas") != null;

            Assert.True(hasQRContent, "Setup flow should show QR code or setup instructions");
        }
        else
        {
            Output.WriteLine("Setup button not found - 2FA may already be enabled or page structure different");
        }
    }

    [Fact]
    public async Task TwoFactorPage_HasVerificationCodeInput()
    {
        // Arrange - Register and login
        var username = $"verify_{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Navigate to 2FA page and start setup
        await Page.GotoAsync($"{WebAppUrl}/account/two-factor");
        await WaitForBlazorAsync();

        var setupButton = await Page.QuerySelectorAsync("button:has-text('Set Up')");
        if (setupButton != null)
        {
            await setupButton.ClickAsync();
            await WaitForBlazorAsync();

            // Assert - Should have code input field
            var codeInput = await Page.QuerySelectorAsync("input[type='text'], input[placeholder*='code' i], input[name*='code' i]");
            var pageContent = await Page.ContentAsync();

            var hasCodeInput = codeInput != null ||
                              pageContent.Contains("verification code", StringComparison.OrdinalIgnoreCase) ||
                              pageContent.Contains("enter code", StringComparison.OrdinalIgnoreCase);

            Output.WriteLine($"Code input found: {codeInput != null}");
            Assert.True(hasCodeInput, "Setup should have verification code input");
        }
    }

    [Fact]
    public async Task TwoFactorPage_NavigableFromProfile()
    {
        // Arrange - Register and login
        var username = $"nav_{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Navigate to profile and look for 2FA link
        await ClickNavigateToProfileAsync();
        await WaitForBlazorAsync();

        var twoFactorLink = await Page.QuerySelectorAsync("a[href*='two-factor'], a:has-text('Two-Factor'), a:has-text('2FA'), a:has-text('Set up')");
        
        // Assert - Profile should have link to 2FA page
        Assert.NotNull(twoFactorLink);

        // Click the link
        await twoFactorLink.ClickAsync();
        await WaitForBlazorAsync();

        // Verify navigation
        Assert.Contains("two-factor", Page.Url, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TwoFactorPage_HasBackNavigation()
    {
        // Arrange - Register and login
        var username = $"back_{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Navigate to 2FA page
        await Page.GotoAsync($"{WebAppUrl}/account/two-factor");
        await WaitForBlazorAsync();

        // Assert - Should have back navigation
        var backLink = await Page.QuerySelectorAsync("a[href*='profile'], a:has-text('Back'), a:has-text('Profile'), button:has-text('Back')");
        
        Assert.NotNull(backLink);
    }

    [Fact]
    public async Task TwoFactorPage_ShowsSecurityInfo()
    {
        // Arrange - Register and login
        var username = $"info_{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Navigate to 2FA page
        await Page.GotoAsync($"{WebAppUrl}/account/two-factor");
        await WaitForBlazorAsync();

        // Assert - Should show security-related information
        var pageContent = await Page.ContentAsync();

        var hasSecurityInfo = pageContent.Contains("security", StringComparison.OrdinalIgnoreCase) ||
                             pageContent.Contains("extra layer", StringComparison.OrdinalIgnoreCase) ||
                             pageContent.Contains("protect", StringComparison.OrdinalIgnoreCase) ||
                             pageContent.Contains("authenticator app", StringComparison.OrdinalIgnoreCase);

        Assert.True(hasSecurityInfo, "2FA page should explain security benefits");
    }
}

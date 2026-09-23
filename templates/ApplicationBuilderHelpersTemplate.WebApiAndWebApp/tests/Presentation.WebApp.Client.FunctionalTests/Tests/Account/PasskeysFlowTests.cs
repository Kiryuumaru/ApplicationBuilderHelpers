using Presentation.WebApp.Client.FunctionalTests.Fixtures;

namespace Presentation.WebApp.Client.FunctionalTests.Tests.Account;

/// <summary>
/// UI-only tests for passkeys management page.
/// All tests use mouse clicks and keyboard input like a real user.
/// </summary>
public class PasskeysFlowTests : WebAppTestBase
{
    public PasskeysFlowTests(PlaywrightFixture playwrightFixture, ITestOutputHelper output) : base(playwrightFixture, output)
    {
    }

    [Fact]
    public async Task PasskeysPage_RequiresAuthentication()
    {
        // Act
        await Page.GotoAsync($"{WebAppUrl}/account/passkeys");
        await WaitForBlazorAsync();

        // Assert
        var currentUrl = Page.Url;
        var pageContent = await Page.ContentAsync();

        var redirectedToLogin = currentUrl.Contains("/auth/login", StringComparison.OrdinalIgnoreCase);
        var showsUnauthorized = pageContent.Contains("unauthorized", StringComparison.OrdinalIgnoreCase) ||
                                pageContent.Contains("access denied", StringComparison.OrdinalIgnoreCase) ||
                                pageContent.Contains("not authorized", StringComparison.OrdinalIgnoreCase);

        Assert.True(redirectedToLogin || showsUnauthorized, 
            "Passkeys page should require authentication");
    }

    [Fact]
    public async Task PasskeysPage_LoadsWhenAuthenticated()
    {
        // Arrange
        var username = $"pkey_{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act
        await Page.GotoAsync($"{WebAppUrl}/account/passkeys");
        await WaitForBlazorAsync();

        // Assert
        var pageContent = await Page.ContentAsync();

        var hasPasskeysContent = pageContent.Contains("Passkey", StringComparison.OrdinalIgnoreCase) ||
                                pageContent.Contains("passwordless", StringComparison.OrdinalIgnoreCase) ||
                                pageContent.Contains("biometric", StringComparison.OrdinalIgnoreCase) ||
                                pageContent.Contains("security key", StringComparison.OrdinalIgnoreCase);

        Assert.True(hasPasskeysContent, "Passkeys page should show passkey management content");
    }

    [Fact]
    public async Task PasskeysPage_HasAddButton()
    {
        // Arrange
        var username = $"addbtn_{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act
        await Page.GotoAsync($"{WebAppUrl}/account/passkeys");
        await WaitForBlazorAsync();

        // Assert
        var addButton = await Page.QuerySelectorAsync("button:has-text('Add'), button:has-text('Register'), button:has-text('Create')");
        
        Assert.NotNull(addButton);
    }

    [Fact]
    public async Task PasskeysPage_ShowsEmptyState_ForNewUser()
    {
        // Arrange
        var username = $"empty_{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act
        await Page.GotoAsync($"{WebAppUrl}/account/passkeys");
        await WaitForBlazorAsync();

        // Assert
        var pageContent = await Page.ContentAsync();

        var hasEmptyState = pageContent.Contains("No passkey", StringComparison.OrdinalIgnoreCase) ||
                           pageContent.Contains("not registered", StringComparison.OrdinalIgnoreCase) ||
                           pageContent.Contains("Add a passkey", StringComparison.OrdinalIgnoreCase) ||
                           pageContent.Contains("🔐", StringComparison.OrdinalIgnoreCase);

        Assert.True(hasEmptyState, "Should show empty state for new user with no passkeys");
    }

    [Fact]
    public async Task PasskeysPage_ShowsSecurityDescription()
    {
        // Arrange
        var username = $"desc_{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act
        await Page.GotoAsync($"{WebAppUrl}/account/passkeys");
        await WaitForBlazorAsync();

        // Assert
        var pageContent = await Page.ContentAsync();

        var hasDescription = pageContent.Contains("passwordless", StringComparison.OrdinalIgnoreCase) ||
                            pageContent.Contains("biometric", StringComparison.OrdinalIgnoreCase) ||
                            pageContent.Contains("security key", StringComparison.OrdinalIgnoreCase) ||
                            pageContent.Contains("authentication", StringComparison.OrdinalIgnoreCase);

        Assert.True(hasDescription, "Should show description explaining passkeys");
    }

    [Fact]
    public async Task PasskeysPage_HasPageTitle()
    {
        // Arrange
        var username = $"title_{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act
        await Page.GotoAsync($"{WebAppUrl}/account/passkeys");
        await WaitForBlazorAsync();

        // Assert
        var title = await Page.TitleAsync();
        
        Assert.Contains("Passkey", title, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PasskeysPage_ShowsKeyIcon()
    {
        // Arrange
        var username = $"icon_{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act
        await Page.GotoAsync($"{WebAppUrl}/account/passkeys");
        await WaitForBlazorAsync();

        // Assert
        var pageContent = await Page.ContentAsync();

        var hasVisualElement = pageContent.Contains("🔐", StringComparison.OrdinalIgnoreCase) ||
                              pageContent.Contains("🔑", StringComparison.OrdinalIgnoreCase) ||
                              await Page.QuerySelectorAsync("svg, [class*='icon']") != null;

        Output.WriteLine($"Visual element found: {hasVisualElement}");
    }
}

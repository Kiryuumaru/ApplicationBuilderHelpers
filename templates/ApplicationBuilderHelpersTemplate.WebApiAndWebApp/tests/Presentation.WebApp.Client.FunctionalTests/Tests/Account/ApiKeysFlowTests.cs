using Presentation.WebApp.Client.FunctionalTests.Fixtures;

namespace Presentation.WebApp.Client.FunctionalTests.Tests.Account;

/// <summary>
/// UI-only tests for API keys management page.
/// All tests use mouse clicks and keyboard input like a real user.
/// </summary>
public class ApiKeysFlowTests : WebAppTestBase
{
    public ApiKeysFlowTests(PlaywrightFixture playwrightFixture, ITestOutputHelper output) : base(playwrightFixture, output)
    {
    }

    [Fact]
    public async Task ApiKeysPage_RequiresAuthentication()
    {
        // Act
        await Page.GotoAsync($"{WebAppUrl}/account/api-keys");
        await WaitForBlazorAsync();

        // Assert
        var currentUrl = Page.Url;
        var pageContent = await Page.ContentAsync();

        var redirectedToLogin = currentUrl.Contains("/auth/login", StringComparison.OrdinalIgnoreCase);
        var showsUnauthorized = pageContent.Contains("unauthorized", StringComparison.OrdinalIgnoreCase) ||
                                pageContent.Contains("access denied", StringComparison.OrdinalIgnoreCase) ||
                                pageContent.Contains("not authorized", StringComparison.OrdinalIgnoreCase);

        Assert.True(redirectedToLogin || showsUnauthorized, 
            "API keys page should require authentication");
    }

    [Fact]
    public async Task ApiKeysPage_LoadsWhenAuthenticated()
    {
        // Arrange
        var username = $"apikey_{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act
        await Page.GotoAsync($"{WebAppUrl}/account/api-keys");
        await WaitForBlazorAsync();

        // Assert
        var pageContent = await Page.ContentAsync();

        var hasApiKeysContent = pageContent.Contains("API Key", StringComparison.OrdinalIgnoreCase) ||
                               pageContent.Contains("API-Key", StringComparison.OrdinalIgnoreCase) ||
                               pageContent.Contains("Create", StringComparison.OrdinalIgnoreCase);

        Assert.True(hasApiKeysContent, "API keys page should show API key management content");
    }

    [Fact]
    public async Task ApiKeysPage_HasCreateButton()
    {
        // Arrange
        var username = $"create_{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act
        await Page.GotoAsync($"{WebAppUrl}/account/api-keys");
        await WaitForBlazorAsync();

        // Assert
        var createButton = await Page.QuerySelectorAsync("button:has-text('Create'), button:has-text('New'), button:has-text('Add')");
        
        Assert.NotNull(createButton);
    }

    [Fact]
    public async Task ApiKeysPage_ShowsEmptyState_ForNewUser()
    {
        // Arrange
        var username = $"empty_{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act
        await Page.GotoAsync($"{WebAppUrl}/account/api-keys");
        await WaitForBlazorAsync();

        // Assert
        var pageContent = await Page.ContentAsync();

        var hasEmptyState = pageContent.Contains("No API key", StringComparison.OrdinalIgnoreCase) ||
                           pageContent.Contains("no keys", StringComparison.OrdinalIgnoreCase) ||
                           pageContent.Contains("Create one", StringComparison.OrdinalIgnoreCase) ||
                           pageContent.Contains("get started", StringComparison.OrdinalIgnoreCase);

        var hasTable = await Page.QuerySelectorAsync("table") != null;
        var hasCreateButton = await Page.QuerySelectorAsync("button:has-text('Create')") != null;

        Assert.True(hasEmptyState || hasCreateButton, "Should show empty state or create option for new user");
    }

    [Fact]
    public async Task ApiKeysPage_ClickCreate_OpensModal()
    {
        // Arrange
        var username = $"modal_{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act
        await Page.GotoAsync($"{WebAppUrl}/account/api-keys");
        await WaitForBlazorAsync();

        var createButton = await Page.QuerySelectorAsync("button:has-text('Create'), button:has-text('New')");
        if (createButton != null)
        {
            await createButton.ClickAsync();
            await WaitForBlazorAsync();

            // Assert
            var pageContent = await Page.ContentAsync();

            var hasModal = pageContent.Contains("Key Name", StringComparison.OrdinalIgnoreCase) ||
                          pageContent.Contains("Expir", StringComparison.OrdinalIgnoreCase) ||
                          await Page.QuerySelectorAsync("input[type='text']") != null;

            Assert.True(hasModal, "Should show create key form or modal");
        }
    }

    [Fact]
    public async Task ApiKeysPage_CreateModal_HasNameInput()
    {
        // Arrange
        var username = $"name_{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act
        await Page.GotoAsync($"{WebAppUrl}/account/api-keys");
        await WaitForBlazorAsync();

        var createButton = await Page.QuerySelectorAsync("button:has-text('Create'), button:has-text('New')");
        if (createButton != null)
        {
            await createButton.ClickAsync();
            await WaitForBlazorAsync();

            // Assert
            var nameInput = await Page.QuerySelectorAsync("input[type='text'], input[id*='name' i]");
            
            Assert.NotNull(nameInput);
        }
    }

    [Fact]
    public async Task ApiKeysPage_CreateModal_HasExpirationOptions()
    {
        // Arrange
        var username = $"expiry_{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act
        await Page.GotoAsync($"{WebAppUrl}/account/api-keys");
        await WaitForBlazorAsync();

        var createButton = await Page.QuerySelectorAsync("button:has-text('Create'), button:has-text('New')");
        if (createButton != null)
        {
            await createButton.ClickAsync();
            await WaitForBlazorAsync();

            // Assert
            var expirySelect = await Page.QuerySelectorAsync("select");
            var pageContent = await Page.ContentAsync();

            var hasExpiryOptions = expirySelect != null ||
                                  pageContent.Contains("Expir", StringComparison.OrdinalIgnoreCase) ||
                                  pageContent.Contains("30 day", StringComparison.OrdinalIgnoreCase) ||
                                  pageContent.Contains("Never", StringComparison.OrdinalIgnoreCase);

            Assert.True(hasExpiryOptions, "Should have expiration options");
        }
    }

    [Fact]
    public async Task ApiKeysPage_HasPageTitle()
    {
        // Arrange
        var username = $"title_{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act
        await Page.GotoAsync($"{WebAppUrl}/account/api-keys");
        await WaitForBlazorAsync();

        // Assert
        var title = await Page.TitleAsync();
        
        Assert.Contains("API", title, StringComparison.OrdinalIgnoreCase);
    }
}

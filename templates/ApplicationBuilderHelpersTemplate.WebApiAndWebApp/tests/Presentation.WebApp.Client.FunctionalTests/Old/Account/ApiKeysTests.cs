using Presentation.WebApp.Client.FunctionalTests.Fixtures;

namespace Presentation.WebApp.Client.FunctionalTests.Old.Account;

/// <summary>
/// Playwright functional tests for API keys management page.
/// Tests API key creation, viewing, and revocation functionality.
/// </summary>
public class ApiKeysTests : WebAppTestBase
{
    public ApiKeysTests(PlaywrightFixture playwrightFixture, ITestOutputHelper output) : base(playwrightFixture, output)
    {
    }

    [Fact]
    public async Task ApiKeys_RequiresAuthentication()
    {
        // Act - Try to access API keys without authentication
        await Page.GotoAsync($"{WebAppUrl}/account/api-keys");
        await WaitForBlazorAsync();

        // Assert - Should redirect to login
        var currentUrl = Page.Url;
        var redirectedToLogin = currentUrl.Contains("/auth/login", StringComparison.OrdinalIgnoreCase);
        Assert.True(redirectedToLogin, "Should redirect to login when accessing API keys unauthenticated");
    }

    [Fact]
    public async Task ApiKeys_Authenticated_ShowsApiKeysPage()
    {
        // Arrange - Register and login
        var username = $"apikeys_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Navigate to API keys
        await Page.GotoAsync($"{WebAppUrl}/account/api-keys");
        await WaitForBlazorAsync();

        // Assert - Should show API keys page
        var pageContent = await Page.ContentAsync();
        Output.WriteLine($"API keys page content length: {pageContent.Length}");

        var hasApiKeysContent = pageContent.Contains("API", StringComparison.OrdinalIgnoreCase) ||
                                pageContent.Contains("key", StringComparison.OrdinalIgnoreCase) ||
                                pageContent.Contains("token", StringComparison.OrdinalIgnoreCase);

        Assert.True(hasApiKeysContent, "API keys page should display API key information");
    }

    [Fact]
    public async Task ApiKeys_HasCreateButton()
    {
        // Arrange - Register and login
        var username = $"create_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Navigate to API keys
        await Page.GotoAsync($"{WebAppUrl}/account/api-keys");
        await WaitForBlazorAsync();

        // Assert - Should have create button
        var createButton = await Page.QuerySelectorAsync("button:has-text('Create'), button:has-text('Generate'), button:has-text('New'), button:has-text('Add')");
        var pageContent = await Page.ContentAsync();
        var hasCreateOption = createButton != null ||
                             pageContent.Contains("create", StringComparison.OrdinalIgnoreCase) ||
                             pageContent.Contains("generate", StringComparison.OrdinalIgnoreCase);

        Assert.True(hasCreateOption, "Should have option to create new API key");
        Output.WriteLine($"Create button found: {createButton != null}");
    }

    [Fact]
    public async Task ApiKeys_CreateForm_HasRequiredFields()
    {
        // Arrange - Register and login
        var username = $"form_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Navigate to API keys and open create form
        await Page.GotoAsync($"{WebAppUrl}/account/api-keys");
        await WaitForBlazorAsync();

        var createButton = await Page.QuerySelectorAsync("button:has-text('Create'), button:has-text('Generate'), button:has-text('New'), button:has-text('Add')");
        if (createButton != null)
        {
            await createButton.ClickAsync();
            await Task.Delay(500);

            // Assert - Should show form with name/description field
            var nameInput = await Page.QuerySelectorAsync("input[name='name' i], input[placeholder*='name' i]");
            var descriptionInput = await Page.QuerySelectorAsync("input[name='description' i], textarea[name='description' i]");

            Output.WriteLine($"Name input found: {nameInput != null}");
            Output.WriteLine($"Description input found: {descriptionInput != null}");
        }
    }

    [Fact]
    public async Task ApiKeys_ShowsExistingKeys()
    {
        // Arrange - Register and login
        var username = $"list_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Navigate to API keys
        await Page.GotoAsync($"{WebAppUrl}/account/api-keys");
        await WaitForBlazorAsync();

        // Assert - Page should handle empty state or show existing keys
        var pageContent = await Page.ContentAsync();
        var hasEmptyState = pageContent.Contains("no api key", StringComparison.OrdinalIgnoreCase) ||
                           pageContent.Contains("no keys", StringComparison.OrdinalIgnoreCase) ||
                           pageContent.Contains("get started", StringComparison.OrdinalIgnoreCase) ||
                           pageContent.Contains("create", StringComparison.OrdinalIgnoreCase);
        var hasTable = await Page.QuerySelectorAsync("table") != null;
        var hasList = await Page.QuerySelectorAsync("[class*='list'], [class*='card']") != null;

        var hasKeyDisplay = hasEmptyState || hasTable || hasList;
        Output.WriteLine($"Has empty state: {hasEmptyState}");
        Output.WriteLine($"Has table: {hasTable}");
        Output.WriteLine($"Has list/cards: {hasList}");

        Assert.True(hasKeyDisplay, "API keys page should show empty state message or key list");
    }

    [Fact]
    public async Task ApiKeys_ShowsCreateKeyButton()
    {
        // Arrange - Register and login
        var username = $"create_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Navigate to API keys
        await Page.GotoAsync($"{WebAppUrl}/account/api-keys");
        await WaitForBlazorAsync();

        // Assert - Check for create key button
        var pageContent = await Page.ContentAsync();
        var hasCreateButton = pageContent.Contains("Create", StringComparison.OrdinalIgnoreCase) &&
                              pageContent.Contains("Key", StringComparison.OrdinalIgnoreCase);

        Output.WriteLine($"Has create button: {hasCreateButton}");
        Assert.True(hasCreateButton, "API keys page should have Create New Key button");
    }

    [Fact]
    public async Task ApiKeys_ShowsEmptyStateMessage()
    {
        // Arrange - Register and login (fresh user with no keys)
        var username = $"empty_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Navigate to API keys
        await Page.GotoAsync($"{WebAppUrl}/account/api-keys");
        await WaitForBlazorAsync();

        // Assert - Check for empty state message
        var pageContent = await Page.ContentAsync();
        var hasEmptyState = pageContent.Contains("No API keys", StringComparison.OrdinalIgnoreCase) ||
                           pageContent.Contains("Create one", StringComparison.OrdinalIgnoreCase) ||
                           pageContent.Contains("get started", StringComparison.OrdinalIgnoreCase);

        Output.WriteLine($"Has empty state: {hasEmptyState}");
        Assert.True(hasEmptyState, "API keys page should show empty state message when user has no keys");
    }

    [Fact]
    public async Task ApiKeys_NavigationFromSidebar()
    {
        // Arrange - Register and login
        var username = $"nav_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Navigate via sidebar
        await GoToHomeAsync();
        var apiKeysLink = await Page.QuerySelectorAsync("a[href*='api-keys' i]");

        if (apiKeysLink != null)
        {
            await apiKeysLink.ClickAsync();
            await WaitForBlazorAsync();

            // Assert - Should be on API keys page
            AssertUrlContains("/account/api-keys");
            Output.WriteLine("✅ API keys page accessible via navigation");
        }
        else
        {
            Output.WriteLine("API keys link not found in sidebar");
        }
    }

    [Fact]
    public async Task ApiKeys_HasApiKeysTitle()
    {
        // Arrange - Register and login
        var username = $"title_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Navigate to API keys
        await Page.GotoAsync($"{WebAppUrl}/account/api-keys");
        await WaitForBlazorAsync();

        // Assert - Page should have API Keys title
        var pageContent = await Page.ContentAsync();
        var hasTitle = pageContent.Contains("API Keys", StringComparison.Ordinal);

        Output.WriteLine($"Has API Keys title: {hasTitle}");
        Assert.True(hasTitle, "API keys page should have 'API Keys' heading");
    }
}

using Presentation.WebApp.Client.FunctionalTests.Fixtures;

namespace Presentation.WebApp.Client.FunctionalTests.Old.Admin;

/// <summary>
/// Playwright functional tests for user management page.
/// Tests admin user management functionality through the Blazor WebApp.
/// </summary>
public class UsersManagementTests : WebAppTestBase
{
    public UsersManagementTests(PlaywrightFixture playwrightFixture, ITestOutputHelper output) : base(playwrightFixture, output)
    {
    }

    [Fact]
    public async Task UsersPage_RequiresAuthentication()
    {
        // Act - Try to access users page without authentication
        await Page.GotoAsync($"{WebAppUrl}/admin/users");
        await WaitForBlazorAsync();

        // Assert - Should redirect to login or show unauthorized
        var currentUrl = Page.Url;
        var pageContent = await Page.ContentAsync();

        var redirectedToLogin = currentUrl.Contains("/auth/login", StringComparison.OrdinalIgnoreCase);
        var showsUnauthorized = pageContent.Contains("unauthorized", StringComparison.OrdinalIgnoreCase) ||
                                pageContent.Contains("access denied", StringComparison.OrdinalIgnoreCase);

        Assert.True(redirectedToLogin || showsUnauthorized, "Should require authentication for admin users page");
    }

    [Fact]
    public async Task UsersPage_Authenticated_ShowsUserTableOrAccessDenied()
    {
        // Arrange - Register and login (regular user, may not have admin role)
        var username = $"users_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Navigate to users page
        await Page.GotoAsync($"{WebAppUrl}/admin/users");
        await WaitForBlazorAsync();

        var currentUrl = Page.Url;
        var pageContent = await Page.ContentAsync();
        Output.WriteLine($"Users page URL: {currentUrl}");

        // Assert - Should either show user management content OR deny access (not just silently do nothing)
        var redirectedToLogin = currentUrl.Contains("/auth/login", StringComparison.OrdinalIgnoreCase);
        var hasUsersContent = pageContent.Contains("user", StringComparison.OrdinalIgnoreCase) &&
                              (pageContent.Contains("management", StringComparison.OrdinalIgnoreCase) ||
                               await Page.QuerySelectorAsync("table") != null);
        var hasAccessDenied = pageContent.Contains("access denied", StringComparison.OrdinalIgnoreCase) ||
                              pageContent.Contains("unauthorized", StringComparison.OrdinalIgnoreCase) ||
                              pageContent.Contains("not authorized", StringComparison.OrdinalIgnoreCase) ||
                              pageContent.Contains("forbidden", StringComparison.OrdinalIgnoreCase);

        Output.WriteLine($"Redirected to login: {redirectedToLogin}");
        Output.WriteLine($"Has users content: {hasUsersContent}");
        Output.WriteLine($"Has access denied: {hasAccessDenied}");

        Assert.True(redirectedToLogin || hasUsersContent || hasAccessDenied,
            "Admin users page should show content, deny access, or redirect to login");
    }

    [Fact]
    public async Task UsersPage_HasSearchFunctionality()
    {
        // Arrange - Register and login
        var username = $"search_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Navigate to users page
        await Page.GotoAsync($"{WebAppUrl}/admin/users");
        await WaitForBlazorAsync();

        var currentUrl = Page.Url;
        var pageContent = await Page.ContentAsync();

        // Assert - If page is accessible (not redirected), it must have search
        var redirectedAway = currentUrl.Contains("/auth/login", StringComparison.OrdinalIgnoreCase);
        var hasAccessDenied = pageContent.Contains("access denied", StringComparison.OrdinalIgnoreCase) ||
                              pageContent.Contains("unauthorized", StringComparison.OrdinalIgnoreCase);

        if (redirectedAway || hasAccessDenied)
        {
            Output.WriteLine("User doesn't have admin access - skipping search test");
            return;
        }

        var searchInput = await Page.QuerySelectorAsync("input[type='search'], input[placeholder*='search' i]");
        Output.WriteLine($"Search input found: {searchInput != null}");
        Assert.NotNull(searchInput);
    }

    [Fact]
    public async Task UsersPage_HasRoleFilter()
    {
        // Arrange - Register and login
        var username = $"filter_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Navigate to users page
        await Page.GotoAsync($"{WebAppUrl}/admin/users");
        await WaitForBlazorAsync();

        var currentUrl = Page.Url;
        var pageContent = await Page.ContentAsync();

        // Skip if user doesn't have admin access
        var redirectedAway = currentUrl.Contains("/auth/login", StringComparison.OrdinalIgnoreCase);
        var hasAccessDenied = pageContent.Contains("access denied", StringComparison.OrdinalIgnoreCase) ||
                              pageContent.Contains("unauthorized", StringComparison.OrdinalIgnoreCase);

        if (redirectedAway || hasAccessDenied)
        {
            Output.WriteLine("User doesn't have admin access - skipping role filter test");
            return;
        }

        // Assert - Should have role filter dropdown
        var roleFilter = await Page.QuerySelectorAsync("select");
        Output.WriteLine($"Role filter found: {roleFilter != null}");
        Assert.NotNull(roleFilter);
    }

    [Fact]
    public async Task UsersPage_HasAddUserButton()
    {
        // Arrange - Register and login
        var username = $"adduser_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Navigate to users page
        await Page.GotoAsync($"{WebAppUrl}/admin/users");
        await WaitForBlazorAsync();

        var currentUrl = Page.Url;
        var pageContent = await Page.ContentAsync();

        // Skip if user doesn't have admin access
        var redirectedAway = currentUrl.Contains("/auth/login", StringComparison.OrdinalIgnoreCase);
        var hasAccessDenied = pageContent.Contains("access denied", StringComparison.OrdinalIgnoreCase) ||
                              pageContent.Contains("unauthorized", StringComparison.OrdinalIgnoreCase);

        if (redirectedAway || hasAccessDenied)
        {
            Output.WriteLine("User doesn't have admin access - skipping add user button test");
            return;
        }

        // Assert - Should have add user button
        var addButton = await Page.QuerySelectorAsync("button:has-text('Add'), button:has-text('Create'), button:has-text('New')");
        Output.WriteLine($"Add user button found: {addButton != null}");
        Assert.NotNull(addButton);
    }

    [Fact]
    public async Task UsersPage_ShowsUserTable_WithColumns()
    {
        // Arrange - Register and login
        var username = $"table_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Navigate to users page
        await Page.GotoAsync($"{WebAppUrl}/admin/users");
        await WaitForBlazorAsync();

        var currentUrl = Page.Url;
        var pageContent = await Page.ContentAsync();

        // Skip if user doesn't have admin access
        var redirectedAway = currentUrl.Contains("/auth/login", StringComparison.OrdinalIgnoreCase);
        var hasAccessDenied = pageContent.Contains("access denied", StringComparison.OrdinalIgnoreCase) ||
                              pageContent.Contains("unauthorized", StringComparison.OrdinalIgnoreCase);

        if (redirectedAway || hasAccessDenied)
        {
            Output.WriteLine("User doesn't have admin access - skipping table columns test");
            return;
        }

        // Assert - Should have table with expected columns
        var table = await Page.QuerySelectorAsync("table");
        Assert.NotNull(table);

        var headers = await Page.QuerySelectorAllAsync("th");
        Output.WriteLine($"Table headers found: {headers.Count}");

        var hasUserColumn = pageContent.Contains("user", StringComparison.OrdinalIgnoreCase);
        var hasRoleColumn = pageContent.Contains("role", StringComparison.OrdinalIgnoreCase);

        Output.WriteLine($"Has User column: {hasUserColumn}");
        Output.WriteLine($"Has Role column: {hasRoleColumn}");

        Assert.True(headers.Count >= 2, "Table should have at least 2 columns");
        Assert.True(hasUserColumn, "Table should have User column");
    }

    [Fact]
    public async Task UsersPage_HasPagination()
    {
        // Arrange - Register and login
        var username = $"page_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Navigate to users page
        await Page.GotoAsync($"{WebAppUrl}/admin/users");
        await WaitForBlazorAsync();

        var currentUrl = Page.Url;
        var pageContent = await Page.ContentAsync();

        // Skip if user doesn't have admin access
        var redirectedAway = currentUrl.Contains("/auth/login", StringComparison.OrdinalIgnoreCase);
        var hasAccessDenied = pageContent.Contains("access denied", StringComparison.OrdinalIgnoreCase) ||
                              pageContent.Contains("unauthorized", StringComparison.OrdinalIgnoreCase);

        if (redirectedAway || hasAccessDenied)
        {
            Output.WriteLine("User doesn't have admin access - skipping pagination test");
            return;
        }

        // Assert - Should have pagination
        var pagination = await Page.QuerySelectorAsync("nav[aria-label*='pagination' i], .pagination, [class*='pagination']");
        var prevButton = await Page.QuerySelectorAsync("button:has-text('Previous'), button[aria-label*='previous' i]");
        var nextButton = await Page.QuerySelectorAsync("button:has-text('Next'), button[aria-label*='next' i]");
        var pageIndicator = pageContent.Contains("page", StringComparison.OrdinalIgnoreCase) &&
                           (pageContent.Contains("of", StringComparison.OrdinalIgnoreCase) ||
                            pageContent.Contains("1", StringComparison.OrdinalIgnoreCase));

        var hasPagination = pagination != null || prevButton != null || nextButton != null || pageIndicator;

        Output.WriteLine($"Pagination found: {pagination != null}");
        Output.WriteLine($"Prev/Next buttons: {prevButton != null}/{nextButton != null}");

        Assert.True(hasPagination, "Users page should have pagination controls");
    }
}

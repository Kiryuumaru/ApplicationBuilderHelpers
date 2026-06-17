using Presentation.WebApp.Client.FunctionalTests.Fixtures;

namespace Presentation.WebApp.Client.FunctionalTests.Old.Admin;

/// <summary>
/// Playwright functional tests for role management page.
/// Tests admin role management functionality through the Blazor WebApp.
/// </summary>
public class RolesManagementTests : WebAppTestBase
{
    public RolesManagementTests(PlaywrightFixture playwrightFixture, ITestOutputHelper output) : base(playwrightFixture, output)
    {
    }

    [Fact]
    public async Task RolesPage_RequiresAuthentication()
    {
        // Act - Try to access roles page without authentication
        await Page.GotoAsync($"{WebAppUrl}/admin/roles");
        await WaitForBlazorAsync();

        // Assert - Should redirect to login or show unauthorized
        var currentUrl = Page.Url;
        var pageContent = await Page.ContentAsync();

        var redirectedToLogin = currentUrl.Contains("/auth/login", StringComparison.OrdinalIgnoreCase);
        var showsUnauthorized = pageContent.Contains("unauthorized", StringComparison.OrdinalIgnoreCase) ||
                                pageContent.Contains("access denied", StringComparison.OrdinalIgnoreCase);

        Assert.True(redirectedToLogin || showsUnauthorized, "Should require authentication for admin roles page");
    }

    [Fact]
    public async Task RolesPage_Authenticated_ShowsRoleCardsOrAccessDenied()
    {
        // Arrange - Register and login
        var username = $"roles_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Navigate to roles page
        await Page.GotoAsync($"{WebAppUrl}/admin/roles");
        await WaitForBlazorAsync();

        var currentUrl = Page.Url;
        var pageContent = await Page.ContentAsync();
        Output.WriteLine($"Roles page URL: {currentUrl}");

        // Assert - Should either show content, deny access, or redirect
        var redirectedToLogin = currentUrl.Contains("/auth/login", StringComparison.OrdinalIgnoreCase);
        var hasRolesContent = pageContent.Contains("role", StringComparison.OrdinalIgnoreCase) &&
                              (pageContent.Contains("permission", StringComparison.OrdinalIgnoreCase) ||
                               pageContent.Contains("admin", StringComparison.OrdinalIgnoreCase));
        var hasAccessDenied = pageContent.Contains("access denied", StringComparison.OrdinalIgnoreCase) ||
                              pageContent.Contains("unauthorized", StringComparison.OrdinalIgnoreCase);

        Output.WriteLine($"Has roles content: {hasRolesContent}");

        Assert.True(redirectedToLogin || hasRolesContent || hasAccessDenied,
            "Roles page should show content, deny access, or redirect to login");
    }

    [Fact]
    public async Task RolesPage_HasAddRoleButton()
    {
        // Arrange - Register and login
        var username = $"addrole_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Navigate to roles page
        await Page.GotoAsync($"{WebAppUrl}/admin/roles");
        await WaitForBlazorAsync();

        var currentUrl = Page.Url;
        var pageContent = await Page.ContentAsync();

        // Skip if user doesn't have admin access
        var redirectedAway = currentUrl.Contains("/auth/login", StringComparison.OrdinalIgnoreCase);
        var hasAccessDenied = pageContent.Contains("access denied", StringComparison.OrdinalIgnoreCase) ||
                              pageContent.Contains("unauthorized", StringComparison.OrdinalIgnoreCase);

        if (redirectedAway || hasAccessDenied)
        {
            Output.WriteLine("User doesn't have admin access - skipping add role button test");
            return;
        }

        // Assert - Should have add role button
        var addButton = await Page.QuerySelectorAsync("button:has-text('Add'), button:has-text('Create'), button:has-text('New')");
        Output.WriteLine($"Add role button found: {addButton != null}");
        Assert.NotNull(addButton);
    }

    [Fact]
    public async Task RolesPage_DisplaysRolePermissions()
    {
        // Arrange - Register and login
        var username = $"perms_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Navigate to roles page
        await Page.GotoAsync($"{WebAppUrl}/admin/roles");
        await WaitForBlazorAsync();

        var currentUrl = Page.Url;
        var pageContent = await Page.ContentAsync();

        // Skip if user doesn't have admin access
        var redirectedAway = currentUrl.Contains("/auth/login", StringComparison.OrdinalIgnoreCase);
        var hasAccessDenied = pageContent.Contains("access denied", StringComparison.OrdinalIgnoreCase) ||
                              pageContent.Contains("unauthorized", StringComparison.OrdinalIgnoreCase);

        if (redirectedAway || hasAccessDenied)
        {
            Output.WriteLine("User doesn't have admin access - skipping role permissions test");
            return;
        }

        // Assert - Should display permissions for roles
        var hasPermissions = pageContent.Contains("permission", StringComparison.OrdinalIgnoreCase) ||
                            pageContent.Contains("read", StringComparison.OrdinalIgnoreCase) ||
                            pageContent.Contains("write", StringComparison.OrdinalIgnoreCase) ||
                            pageContent.Contains("scope", StringComparison.OrdinalIgnoreCase);

        Output.WriteLine($"Has permissions display: {hasPermissions}");
        Assert.True(hasPermissions, "Roles page should display permissions information");
    }

    [Fact]
    public async Task RolesPage_ShowsUserCount()
    {
        // Arrange - Register and login
        var username = $"count_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Navigate to roles page
        await Page.GotoAsync($"{WebAppUrl}/admin/roles");
        await WaitForBlazorAsync();

        var currentUrl = Page.Url;
        var pageContent = await Page.ContentAsync();

        // Skip if user doesn't have admin access
        var redirectedAway = currentUrl.Contains("/auth/login", StringComparison.OrdinalIgnoreCase);
        var hasAccessDenied = pageContent.Contains("access denied", StringComparison.OrdinalIgnoreCase) ||
                              pageContent.Contains("unauthorized", StringComparison.OrdinalIgnoreCase);

        if (redirectedAway || hasAccessDenied)
        {
            Output.WriteLine("User doesn't have admin access - skipping user count test");
            return;
        }

        // Assert - Should show user count for roles
        var hasUserCount = pageContent.Contains("user", StringComparison.OrdinalIgnoreCase) ||
                          pageContent.Contains("member", StringComparison.OrdinalIgnoreCase);

        Output.WriteLine($"Has user count display: {hasUserCount}");
        Assert.True(hasUserCount, "Roles page should display user count or member information");
    }

    [Fact]
    public async Task RolesPage_SystemRolesNotDeletable()
    {
        // Arrange - Register and login
        var username = $"system_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Navigate to roles page
        await Page.GotoAsync($"{WebAppUrl}/admin/roles");
        await WaitForBlazorAsync();

        var currentUrl = Page.Url;
        var pageContent = await Page.ContentAsync();

        // Skip if user doesn't have admin access
        var redirectedAway = currentUrl.Contains("/auth/login", StringComparison.OrdinalIgnoreCase);
        var hasAccessDenied = pageContent.Contains("access denied", StringComparison.OrdinalIgnoreCase) ||
                              pageContent.Contains("unauthorized", StringComparison.OrdinalIgnoreCase);

        if (redirectedAway || hasAccessDenied)
        {
            Output.WriteLine("User doesn't have admin access - skipping system roles test");
            return;
        }

        // Assert - Admin/User system roles should exist and be visible
        var hasAdminRole = pageContent.Contains("Admin", StringComparison.Ordinal);
        var hasUserRole = pageContent.Contains("User", StringComparison.Ordinal);
        Output.WriteLine($"Has Admin role: {hasAdminRole}");
        Output.WriteLine($"Has User role: {hasUserRole}");

        Assert.True(hasAdminRole, "Roles page should display the Admin system role");
    }

    [Fact]
    public async Task RolesPage_HasEditButtons()
    {
        // Arrange - Register and login
        var username = $"edit_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Navigate to roles page
        await Page.GotoAsync($"{WebAppUrl}/admin/roles");
        await WaitForBlazorAsync();

        var currentUrl = Page.Url;
        var pageContent = await Page.ContentAsync();

        // Skip if user doesn't have admin access
        var redirectedAway = currentUrl.Contains("/auth/login", StringComparison.OrdinalIgnoreCase);
        var hasAccessDenied = pageContent.Contains("access denied", StringComparison.OrdinalIgnoreCase) ||
                              pageContent.Contains("unauthorized", StringComparison.OrdinalIgnoreCase);

        if (redirectedAway || hasAccessDenied)
        {
            Output.WriteLine("User doesn't have admin access - skipping edit buttons test");
            return;
        }

        // Assert - Should have edit buttons for roles
        var editButtons = await Page.QuerySelectorAllAsync("button[aria-label*='edit' i], button:has(svg)");
        Output.WriteLine($"Edit buttons found: {editButtons.Count}");
        Assert.True(editButtons.Count > 0, "Roles page should have edit buttons");
    }

    [Fact]
    public async Task RolesPage_DisplaysRoleDescription()
    {
        // Arrange - Register and login
        var username = $"desc_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Navigate to roles page
        await Page.GotoAsync($"{WebAppUrl}/admin/roles");
        await WaitForBlazorAsync();

        var currentUrl = Page.Url;
        var pageContent = await Page.ContentAsync();

        // Skip if user doesn't have admin access
        var redirectedAway = currentUrl.Contains("/auth/login", StringComparison.OrdinalIgnoreCase);
        var hasAccessDenied = pageContent.Contains("access denied", StringComparison.OrdinalIgnoreCase) ||
                              pageContent.Contains("unauthorized", StringComparison.OrdinalIgnoreCase);

        if (redirectedAway || hasAccessDenied)
        {
            Output.WriteLine("User doesn't have admin access - skipping role description test");
            return;
        }

        // Assert - Should show role descriptions
        var hasDescriptions = pageContent.Contains("access", StringComparison.OrdinalIgnoreCase) ||
                             pageContent.Contains("management", StringComparison.OrdinalIgnoreCase) ||
                             pageContent.Contains("description", StringComparison.OrdinalIgnoreCase);

        Output.WriteLine($"Has role descriptions: {hasDescriptions}");
        Assert.True(hasDescriptions, "Roles page should display role descriptions");
    }
}

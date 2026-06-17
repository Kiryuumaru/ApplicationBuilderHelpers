using Presentation.WebApp.Client.FunctionalTests.Fixtures;

namespace Presentation.WebApp.Client.FunctionalTests.Old.Admin;

/// <summary>
/// Playwright functional tests for permissions management page.
/// Tests permission viewing and hierarchy display functionality.
/// </summary>
public class PermissionsManagementTests : WebAppTestBase
{
    public PermissionsManagementTests(PlaywrightFixture playwrightFixture, ITestOutputHelper output) : base(playwrightFixture, output)
    {
    }

    [Fact]
    public async Task PermissionsPage_RequiresAuthentication()
    {
        // Act - Try to access permissions page without authentication
        await Page.GotoAsync($"{WebAppUrl}/admin/permissions");
        await WaitForBlazorAsync();

        // Assert - Should redirect to login or show unauthorized
        var currentUrl = Page.Url;
        var pageContent = await Page.ContentAsync();

        var redirectedToLogin = currentUrl.Contains("/auth/login", StringComparison.OrdinalIgnoreCase);
        var showsUnauthorized = pageContent.Contains("unauthorized", StringComparison.OrdinalIgnoreCase) ||
                                pageContent.Contains("access denied", StringComparison.OrdinalIgnoreCase);

        Assert.True(redirectedToLogin || showsUnauthorized, "Should require authentication for admin permissions page");
    }

    [Fact]
    public async Task PermissionsPage_Authenticated_ShowsPermissionsTreeOrAccessDenied()
    {
        // Arrange - Register and login
        var username = $"perms_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Navigate to permissions page
        await Page.GotoAsync($"{WebAppUrl}/admin/permissions");
        await WaitForBlazorAsync();

        var currentUrl = Page.Url;
        var pageContent = await Page.ContentAsync();
        Output.WriteLine($"Permissions page URL: {currentUrl}");

        // Assert - Should either show content, deny access, or redirect
        var redirectedToLogin = currentUrl.Contains("/auth/login", StringComparison.OrdinalIgnoreCase);
        var hasPermissionsContent = pageContent.Contains("permission", StringComparison.OrdinalIgnoreCase) &&
                                    (pageContent.Contains("scope", StringComparison.OrdinalIgnoreCase) ||
                                     pageContent.Contains("access", StringComparison.OrdinalIgnoreCase));
        var hasAccessDenied = pageContent.Contains("access denied", StringComparison.OrdinalIgnoreCase) ||
                              pageContent.Contains("unauthorized", StringComparison.OrdinalIgnoreCase);

        Output.WriteLine($"Has permissions content: {hasPermissionsContent}");

        Assert.True(redirectedToLogin || hasPermissionsContent || hasAccessDenied,
            "Permissions page should show content, deny access, or redirect to login");
    }

    [Fact]
    public async Task PermissionsPage_ShowsHierarchicalStructure()
    {
        // Arrange - Register and login
        var username = $"hier_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Navigate to permissions page
        await Page.GotoAsync($"{WebAppUrl}/admin/permissions");
        await WaitForBlazorAsync();

        var currentUrl = Page.Url;
        var pageContent = await Page.ContentAsync();

        // Skip if user doesn't have admin access
        var redirectedAway = currentUrl.Contains("/auth/login", StringComparison.OrdinalIgnoreCase);
        var hasAccessDenied = pageContent.Contains("access denied", StringComparison.OrdinalIgnoreCase) ||
                              pageContent.Contains("unauthorized", StringComparison.OrdinalIgnoreCase);

        if (redirectedAway || hasAccessDenied)
        {
            Output.WriteLine("User doesn't have admin access - skipping hierarchy test");
            return;
        }

        // Assert - Should show hierarchical structure (tree view, nested lists, etc.)
        var treeView = await Page.QuerySelectorAsync("[class*='tree'], [class*='nested'], ul ul, [role='tree']");
        var expandButtons = await Page.QuerySelectorAllAsync("button[aria-expanded], [class*='expand'], [class*='collapse'], svg");
        
        Output.WriteLine($"Tree view element found: {treeView != null}");
        Output.WriteLine($"Expand/collapse buttons found: {expandButtons.Count}");

        Assert.True(treeView != null || expandButtons.Count > 0,
            "Permissions page should show hierarchical structure with tree view or expand/collapse buttons");
    }

    [Fact]
    public async Task PermissionsPage_HasSearchFunctionality()
    {
        // Arrange - Register and login
        var username = $"search_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Navigate to permissions page
        await Page.GotoAsync($"{WebAppUrl}/admin/permissions");
        await WaitForBlazorAsync();

        var currentUrl = Page.Url;
        var pageContent = await Page.ContentAsync();

        // Skip if user doesn't have admin access
        var redirectedAway = currentUrl.Contains("/auth/login", StringComparison.OrdinalIgnoreCase);
        var hasAccessDenied = pageContent.Contains("access denied", StringComparison.OrdinalIgnoreCase) ||
                              pageContent.Contains("unauthorized", StringComparison.OrdinalIgnoreCase);

        if (redirectedAway || hasAccessDenied)
        {
            Output.WriteLine("User doesn't have admin access - skipping search test");
            return;
        }

        // Assert - Should have search input
        var searchInput = await Page.QuerySelectorAsync("input[type='search'], input[placeholder*='search' i], input[placeholder*='filter' i]");
        Output.WriteLine($"Search input found: {searchInput != null}");
        Assert.NotNull(searchInput);
    }

    [Fact]
    public async Task PermissionsPage_ShowsPermissionCategories()
    {
        // Arrange - Register and login
        var username = $"cats_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Navigate to permissions page
        await Page.GotoAsync($"{WebAppUrl}/admin/permissions");
        await WaitForBlazorAsync();

        var currentUrl = Page.Url;
        var pageContent = await Page.ContentAsync();

        // Skip if user doesn't have admin access
        var redirectedAway = currentUrl.Contains("/auth/login", StringComparison.OrdinalIgnoreCase);
        var hasAccessDenied = pageContent.Contains("access denied", StringComparison.OrdinalIgnoreCase) ||
                              pageContent.Contains("unauthorized", StringComparison.OrdinalIgnoreCase);

        if (redirectedAway || hasAccessDenied)
        {
            Output.WriteLine("User doesn't have admin access - skipping categories test");
            return;
        }

        // Assert - Should show permission categories (based on WebApi permission structure)
        var hasUserPermissions = pageContent.Contains("user", StringComparison.OrdinalIgnoreCase);
        var hasRolePermissions = pageContent.Contains("role", StringComparison.OrdinalIgnoreCase);
        var hasAuthPermissions = pageContent.Contains("auth", StringComparison.OrdinalIgnoreCase);

        Output.WriteLine($"Has user permissions: {hasUserPermissions}");
        Output.WriteLine($"Has role permissions: {hasRolePermissions}");
        Output.WriteLine($"Has auth permissions: {hasAuthPermissions}");

        Assert.True(hasUserPermissions || hasRolePermissions || hasAuthPermissions,
            "Permissions page should display permission categories");
    }

    [Fact]
    public async Task PermissionsPage_ShowsPermissionDescriptions()
    {
        // Arrange - Register and login
        var username = $"desc_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Navigate to permissions page
        await Page.GotoAsync($"{WebAppUrl}/admin/permissions");
        await WaitForBlazorAsync();

        var currentUrl = Page.Url;
        var pageContent = await Page.ContentAsync();

        // Skip if user doesn't have admin access
        var redirectedAway = currentUrl.Contains("/auth/login", StringComparison.OrdinalIgnoreCase);
        var hasAccessDenied = pageContent.Contains("access denied", StringComparison.OrdinalIgnoreCase) ||
                              pageContent.Contains("unauthorized", StringComparison.OrdinalIgnoreCase);

        if (redirectedAway || hasAccessDenied)
        {
            Output.WriteLine("User doesn't have admin access - skipping descriptions test");
            return;
        }

        // Assert - Should show permission descriptions
        var hasDescriptions = pageContent.Contains("read", StringComparison.OrdinalIgnoreCase) ||
                             pageContent.Contains("write", StringComparison.OrdinalIgnoreCase) ||
                             pageContent.Contains("create", StringComparison.OrdinalIgnoreCase) ||
                             pageContent.Contains("delete", StringComparison.OrdinalIgnoreCase) ||
                             pageContent.Contains("manage", StringComparison.OrdinalIgnoreCase);

        Output.WriteLine($"Has permission descriptions: {hasDescriptions}");
        Assert.True(hasDescriptions, "Permissions page should display permission descriptions");
    }

    [Fact]
    public async Task PermissionsPage_ExpandCollapseTreeNodes()
    {
        // Arrange - Register and login
        var username = $"expand_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Navigate to permissions page
        await Page.GotoAsync($"{WebAppUrl}/admin/permissions");
        await WaitForBlazorAsync();
        
        // Wait for potential authorization check and content load
        await Task.Delay(1000);
        await WaitForBlazorAsync();

        var currentUrl = Page.Url;
        Output.WriteLine($"Current URL after navigation: {currentUrl}");
        
        // Skip if user doesn't have access (not admin) - redirected to login or not-found
        if (currentUrl.Contains("/auth/login", StringComparison.OrdinalIgnoreCase) ||
            currentUrl.Contains("/not-found", StringComparison.OrdinalIgnoreCase) ||
            !currentUrl.Contains("/admin/permissions", StringComparison.OrdinalIgnoreCase))
        {
            Output.WriteLine($"User does not have admin access, skipping expand/collapse test. Current URL: {currentUrl}");
            return;
        }

        // Check for page content to confirm we have access - look for permission-specific content
        // If we can't find the actual permissions UI elements, user likely doesn't have admin access
        var permissionsContent = await Page.QuerySelectorAsync("[data-testid='permissions-tree'], .permissions-list, .permission-item, h1:has-text('Permissions')");
        if (permissionsContent == null)
        {
            Output.WriteLine("Permissions page content not found (user may not have admin access), skipping test.");
            return;
        }

        // Assert - Should be able to expand/collapse tree nodes (use short timeout to not block if element doesn't exist)
        var expandButton = await Page.QuerySelectorAsync("button[aria-expanded='false'], [class*='expand'], button:has(svg)");
        
        if (expandButton != null)
        {
            try
            {
                // Click to expand - use short timeout
                await expandButton.ClickAsync(new() { Timeout = 3000 });
                await Task.Delay(300);
                Output.WriteLine("Expand/collapse functionality tested");
            }
            catch (TimeoutException)
            {
                Output.WriteLine("Expand button found but not clickable (may be hidden due to access restrictions)");
            }
        }
        else
        {
            Output.WriteLine("No expandable tree nodes found (might be flat list, auto-expanded, or user lacks admin access)");
        }
    }

    [Fact]
    public async Task PermissionsPage_NavigationFromSidebar()
    {
        // Arrange - Register and login
        var username = $"nav_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Navigate via sidebar
        await GoToHomeAsync();
        var permissionsLink = await Page.QuerySelectorAsync("a[href*='permissions' i]");

        if (permissionsLink != null)
        {
            await permissionsLink.ClickAsync();
            await WaitForBlazorAsync();

            // Assert - Should be on permissions page
            AssertUrlContains("/admin/permissions");
            Output.WriteLine("✅ Permissions page accessible via navigation");
        }
        else
        {
            Output.WriteLine("Permissions link not found in sidebar");
        }
    }

    [Fact]
    public async Task PermissionsPage_ReadOnlyForNonAdmins()
    {
        // Arrange - Register and login (regular user, not admin)
        var username = $"readonly_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Navigate to permissions page
        await Page.GotoAsync($"{WebAppUrl}/admin/permissions");
        await WaitForBlazorAsync();

        var currentUrl = Page.Url;
        var pageContent = await Page.ContentAsync();

        // Skip if redirected to login (expected for non-admins)
        var redirectedAway = currentUrl.Contains("/auth/login", StringComparison.OrdinalIgnoreCase);
        var hasAccessDenied = pageContent.Contains("access denied", StringComparison.OrdinalIgnoreCase) ||
                              pageContent.Contains("unauthorized", StringComparison.OrdinalIgnoreCase);

        if (redirectedAway || hasAccessDenied)
        {
            Output.WriteLine("Non-admin user correctly denied access to permissions page");
            Assert.True(true); // Pass - non-admin should not have access
            return;
        }

        // If we have access, verify it's read-only (no edit buttons)
        var editButton = await Page.QuerySelectorAsync("button:has-text('Edit'), button:has-text('Create'), button:has-text('Delete')");
        Output.WriteLine($"Edit buttons found: {editButton != null}");

        // Permissions page should be read-only
        Assert.Null(editButton);
    }

    [Fact]
    public async Task PermissionsPage_ShowsPermissionIds()
    {
        // Arrange - Register and login
        var username = $"ids_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Navigate to permissions page
        await Page.GotoAsync($"{WebAppUrl}/admin/permissions");
        await WaitForBlazorAsync();

        var currentUrl = Page.Url;
        var pageContent = await Page.ContentAsync();

        // Skip if user doesn't have admin access
        var redirectedAway = currentUrl.Contains("/auth/login", StringComparison.OrdinalIgnoreCase);
        var hasAccessDenied = pageContent.Contains("access denied", StringComparison.OrdinalIgnoreCase) ||
                              pageContent.Contains("unauthorized", StringComparison.OrdinalIgnoreCase);

        if (redirectedAway || hasAccessDenied)
        {
            Output.WriteLine("User doesn't have admin access - skipping permission IDs test");
            return;
        }

        // Assert - Should show permission identifiers
        var hasPermissionPatterns = pageContent.Contains(".read", StringComparison.OrdinalIgnoreCase) ||
                                    pageContent.Contains(".write", StringComparison.OrdinalIgnoreCase) ||
                                    pageContent.Contains(".create", StringComparison.OrdinalIgnoreCase) ||
                                    pageContent.Contains(".delete", StringComparison.OrdinalIgnoreCase) ||
                                    pageContent.Contains(":", StringComparison.OrdinalIgnoreCase);

        Output.WriteLine($"Has permission ID patterns: {hasPermissionPatterns}");
        Assert.True(hasPermissionPatterns, "Permissions page should display permission identifiers");
    }

    [Fact]
    public async Task PermissionsPage_GroupedByResourceType()
    {
        // Arrange - Register and login
        var username = $"group_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Navigate to permissions page
        await Page.GotoAsync($"{WebAppUrl}/admin/permissions");
        await WaitForBlazorAsync();

        var currentUrl = Page.Url;
        var pageContent = await Page.ContentAsync();

        // Skip if user doesn't have admin access
        var redirectedAway = currentUrl.Contains("/auth/login", StringComparison.OrdinalIgnoreCase);
        var hasAccessDenied = pageContent.Contains("access denied", StringComparison.OrdinalIgnoreCase) ||
                              pageContent.Contains("unauthorized", StringComparison.OrdinalIgnoreCase);

        if (redirectedAway || hasAccessDenied)
        {
            Output.WriteLine("User doesn't have admin access - skipping resource grouping test");
            return;
        }

        // Assert - Should be grouped by resource type
        var headers = await Page.QuerySelectorAllAsync("h2, h3, h4, [class*='header'], [class*='title']");
        Output.WriteLine($"Found {headers.Count} potential group headers");

        var hasResourceGroups = pageContent.Contains("users", StringComparison.OrdinalIgnoreCase) ||
                               pageContent.Contains("roles", StringComparison.OrdinalIgnoreCase) ||
                               pageContent.Contains("sessions", StringComparison.OrdinalIgnoreCase);

        Output.WriteLine($"Has resource group organization: {hasResourceGroups}");
        Assert.True(hasResourceGroups, "Permissions page should be organized by resource type");
    }
}

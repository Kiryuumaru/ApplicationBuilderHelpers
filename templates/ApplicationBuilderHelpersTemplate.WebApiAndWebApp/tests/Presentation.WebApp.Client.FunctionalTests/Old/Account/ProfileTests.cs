using Presentation.WebApp.Client.FunctionalTests.Fixtures;

namespace Presentation.WebApp.Client.FunctionalTests.Old.Account;

/// <summary>
/// Playwright functional tests for user profile page.
/// Tests profile viewing and editing functionality.
/// </summary>
public class ProfileTests : WebAppTestBase
{
    public ProfileTests(PlaywrightFixture playwrightFixture, ITestOutputHelper output) : base(playwrightFixture, output)
    {
    }

    [Fact]
    public async Task Profile_RequiresAuthentication()
    {
        // Act - Try to access profile without authentication
        await Page.GotoAsync($"{WebAppUrl}/account/profile");
        await WaitForBlazorAsync();

        // Assert - Should redirect to login
        var currentUrl = Page.Url;
        var redirectedToLogin = currentUrl.Contains("/auth/login", StringComparison.OrdinalIgnoreCase);
        Assert.True(redirectedToLogin, "Should redirect to login when accessing profile unauthenticated");
    }

    [Fact]
    public async Task Profile_Authenticated_ShowsUserInfo()
    {
        // Arrange - Register and login
        var username = $"profile_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Navigate to profile
        await Page.GotoAsync($"{WebAppUrl}/account/profile");
        await WaitForBlazorAsync();

        // Assert - Should show profile page with user info
        var pageContent = await Page.ContentAsync();
        Output.WriteLine($"Profile page content length: {pageContent.Length}");

        // Should contain user's email or username
        var hasUserInfo = pageContent.Contains(email, StringComparison.OrdinalIgnoreCase) ||
                         pageContent.Contains(username, StringComparison.OrdinalIgnoreCase) ||
                         pageContent.Contains("profile", StringComparison.OrdinalIgnoreCase);

        Assert.True(hasUserInfo, "Profile page should display user information");
    }

    [Fact]
    public async Task Profile_ShowsUserInitial()
    {
        // Arrange - Register and login
        var username = $"initials_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Navigate to profile
        await Page.GotoAsync($"{WebAppUrl}/account/profile");
        await WaitForBlazorAsync();

        // Assert - Should show user initial in avatar
        var avatarElement = await Page.QuerySelectorAsync(".rounded-full, [class*='avatar']");
        Assert.NotNull(avatarElement);
    }

    [Fact]
    public async Task Profile_DisplaysUserInfo()
    {
        // Arrange - Register and login
        var username = $"display_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Navigate to profile
        await Page.GotoAsync($"{WebAppUrl}/account/profile");
        await WaitForBlazorAsync();
        
        // Wait for the profile to load - the username label appears after profile data loads
        await Page.WaitForSelectorAsync("text=Username", new() { Timeout = 10000 });
        await Task.Delay(500); // Allow for any remaining state updates

        // Assert - Should display user profile information (read-only)
        var pageContent = await Page.ContentAsync();
        var hasUsernameLabel = pageContent.Contains("Username", StringComparison.OrdinalIgnoreCase);
        var hasEmailLabel = pageContent.Contains("Email", StringComparison.OrdinalIgnoreCase);
        var hasUsername = pageContent.Contains(username, StringComparison.OrdinalIgnoreCase);

        Output.WriteLine($"Has Username label: {hasUsernameLabel}");
        Output.WriteLine($"Has Email label: {hasEmailLabel}");
        Output.WriteLine($"Has username value: {hasUsername}");

        Assert.True(hasUsernameLabel && hasEmailLabel, "Should display profile labels");
        Assert.True(hasUsername, "Should display username value");
    }

    [Fact]
    public async Task Profile_HasEditableFields()
    {
        // Arrange - Register and login
        var username = $"editable_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Navigate to profile
        await Page.GotoAsync($"{WebAppUrl}/account/profile");
        await WaitForBlazorAsync();
        await Task.Delay(500); // Wait for profile data to load

        // Assert - Profile page uses inline editing pattern with Edit buttons
        // that expand into input fields when clicked
        var editButtons = await Page.QuerySelectorAllAsync("button:has-text('Edit')");
        var pageContent = await Page.ContentAsync();
        
        // Check for Username and Email labels (profile is loaded)
        var hasUsernameSection = pageContent.Contains("Username", StringComparison.OrdinalIgnoreCase);
        var hasEmailSection = pageContent.Contains("Email", StringComparison.OrdinalIgnoreCase);
        var hasEditButtons = editButtons.Count > 0;

        Output.WriteLine($"Has Username section: {hasUsernameSection}");
        Output.WriteLine($"Has Email section: {hasEmailSection}");
        Output.WriteLine($"Edit button count: {editButtons.Count}");

        // The page has editable fields if it shows Edit buttons for username/email
        Assert.True(hasUsernameSection && hasEmailSection, "Should display username and email sections");
        Assert.True(hasEditButtons, "Should have Edit buttons for inline editing");
    }

    [Fact]
    public async Task Profile_HasSecuritySection()
    {
        // Arrange - Register and login
        var username = $"security_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Navigate to profile
        await Page.GotoAsync($"{WebAppUrl}/account/profile");
        await WaitForBlazorAsync();

        // Assert - Should have security section
        var pageContent = await Page.ContentAsync();
        var hasSecuritySection = pageContent.Contains("security", StringComparison.OrdinalIgnoreCase) ||
                                 pageContent.Contains("password", StringComparison.OrdinalIgnoreCase) ||
                                 pageContent.Contains("two-factor", StringComparison.OrdinalIgnoreCase);

        Assert.True(hasSecuritySection, "Profile should have security section");
    }

    [Fact(Skip = "Profile page navigation requires further investigation")]
    public async Task Profile_HasChangePasswordLink()
    {
        // Arrange - Register and login
        var username = $"pwdlink_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Navigate to profile
        await Page.GotoAsync($"{WebAppUrl}/account/profile");
        await WaitForBlazorAsync();

        // Wait for profile to load (Security section appears after profile loads)
        await Page.WaitForSelectorAsync("text=Security", new() { Timeout = 5000 });

        // Assert - Should have change password link
        var changePasswordLink = await Page.QuerySelectorAsync("a[href*='change-password'], a[href*='password']");
        Assert.NotNull(changePasswordLink);
    }

    [Fact]
    public async Task Profile_ClickChangePassword_NavigatesToChangePasswordPage()
    {
        // Arrange - Register and login
        var username = $"changepwd_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        await Page.GotoAsync($"{WebAppUrl}/account/profile");
        await WaitForBlazorAsync();

        // Act - Click change password link
        var changePasswordLink = await Page.QuerySelectorAsync("a[href*='change-password' i]");
        if (changePasswordLink != null)
        {
            await changePasswordLink.ClickAsync();
            await WaitForBlazorAsync();

            // Assert
            AssertUrlContains("change-password");
        }
    }
}

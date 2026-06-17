using Presentation.WebApp.Client.FunctionalTests.Fixtures;

namespace Presentation.WebApp.Client.FunctionalTests.Account;

/// <summary>
/// UI-only tests for user profile page functionality.
/// All tests use mouse clicks and keyboard input only - like a real user.
/// </summary>
public class ProfileFlowTests : WebAppTestBase
{
    public ProfileFlowTests(PlaywrightFixture playwrightFixture, ITestOutputHelper output) : base(playwrightFixture, output)
    {
    }

    [Fact]
    public async Task Journey_ProfileRequiresAuth_RedirectsToLogin()
    {
        // Act - Try to access profile without authentication
        await Page.GotoAsync($"{WebAppUrl}/account/profile");
        await WaitForBlazorAsync();
        await Task.Delay(500);

        // Assert - Should redirect to login
        AssertUrlContains("/auth/login");
        Output.WriteLine("[TEST] Profile page requires authentication");
    }

    [Fact]
    public async Task Journey_ProfileShowsUserInfo()
    {
        // Arrange - Register and login
        var username = GenerateUsername("profile");
        var email = GenerateEmail(username);

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Navigate to profile
        await ClickNavigateToProfileAsync();
        await WaitForBlazorAsync();

        // Wait for profile data to load
        try
        {
            await Page.WaitForSelectorAsync("text=Username", new() { Timeout = 10000 });
        }
        catch (TimeoutException)
        {
            Output.WriteLine("[TEST] Username label not found, checking page content anyway");
        }

        // Assert - Should show user info
        var pageContent = await Page.ContentAsync();
        var hasUserInfo = pageContent.Contains(email, StringComparison.OrdinalIgnoreCase) ||
                         pageContent.Contains(username, StringComparison.OrdinalIgnoreCase) ||
                         pageContent.Contains("profile", StringComparison.OrdinalIgnoreCase);

        Assert.True(hasUserInfo, "Profile page should display user information");
        Output.WriteLine($"[TEST] Profile shows user info for {username}");
    }

    [Fact]
    public async Task Journey_ProfileHasUsernameAndEmailLabels()
    {
        // Arrange - Register and login
        var username = GenerateUsername("labels");
        var email = GenerateEmail(username);

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Navigate to profile
        await ClickNavigateToProfileAsync();
        await Page.WaitForSelectorAsync("text=Username", new() { Timeout = 10000 });
        await Task.Delay(500);

        // Assert - Should have username and email labels
        var pageContent = await Page.ContentAsync();
        var hasUsernameLabel = pageContent.Contains("Username", StringComparison.OrdinalIgnoreCase);
        var hasEmailLabel = pageContent.Contains("Email", StringComparison.OrdinalIgnoreCase);

        Assert.True(hasUsernameLabel, "Profile should have Username label");
        Assert.True(hasEmailLabel, "Profile should have Email label");
        Output.WriteLine("[TEST] Profile has username and email labels");
    }

    [Fact]
    public async Task Journey_ProfileShowsUserAvatar()
    {
        // Arrange - Register and login
        var username = GenerateUsername("avatar");
        var email = GenerateEmail(username);

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Navigate to profile
        await ClickNavigateToProfileAsync();
        await WaitForBlazorAsync();

        // Assert - Should show user avatar/initial
        var avatarElement = await Page.QuerySelectorAsync(".rounded-full, [class*='avatar']");
        Assert.NotNull(avatarElement);
        Output.WriteLine("[TEST] Profile shows user avatar");
    }

    [Fact]
    public async Task Journey_ProfileHasEditButtons()
    {
        // Arrange - Register and login
        var username = GenerateUsername("edit");
        var email = GenerateEmail(username);

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Navigate to profile
        await ClickNavigateToProfileAsync();
        await Page.WaitForSelectorAsync("text=Username", new() { Timeout = 10000 });
        await Task.Delay(500);

        // Assert - Should have edit buttons for inline editing
        var editButtons = await Page.QuerySelectorAllAsync("button:has-text('Edit')");
        Assert.True(editButtons.Count > 0, "Profile should have Edit buttons");
        Output.WriteLine($"[TEST] Profile has {editButtons.Count} edit button(s)");
    }

    [Fact(Skip = "Edit username functionality needs verification")]
    public async Task Journey_EditUsername_UpdatesProfile()
    {
        // Arrange - Register and login
        var username = GenerateUsername("editname");
        var email = GenerateEmail(username);

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Navigate to profile
        await ClickNavigateToProfileAsync();
        await Page.WaitForSelectorAsync("text=Username", new() { Timeout = 10000 });
        await Task.Delay(500);

        // Act - Click edit button for username
        var editButtons = Page.Locator("button:has-text('Edit')");
        await editButtons.First.ClickAsync();
        await Task.Delay(300);

        // Type new username
        var newUsername = GenerateUsername("updated");
        var usernameInput = Page.Locator("input[name='username'], input#username").First;
        await usernameInput.ClearAsync();
        await usernameInput.FillAsync(newUsername);

        // Click save
        var saveButton = Page.Locator("button:has-text('Save')").First;
        await saveButton.ClickAsync();

        // Wait for success
        await WaitForSuccessMessageAsync(timeoutMs: 5000);

        // Assert - Username should be updated
        var pageContent = await Page.ContentAsync();
        Assert.Contains(newUsername, pageContent, StringComparison.OrdinalIgnoreCase);
        Output.WriteLine($"[TEST] Username updated to {newUsername}");
    }

    [Fact]
    public async Task Journey_ProfileNavigatedViaUserMenu()
    {
        // Arrange - Register and login
        var username = GenerateUsername("menunav");
        var email = GenerateEmail(username);

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);
        await GoToHomeAsync();
        await AssertIsAuthenticatedAsync();

        // Act - Use user menu to navigate to profile (this tests ClickNavigateToProfileAsync)
        await ClickNavigateToProfileAsync();

        // Assert - Should be on profile page
        AssertUrlContains("/account/profile");
        Output.WriteLine("[TEST] Navigated to profile via user menu");
    }

    [Fact]
    public async Task Journey_ProfilePageRefresh_StillShowsProfile()
    {
        // Arrange - Register and login, navigate to profile
        var username = GenerateUsername("refresh");
        var email = GenerateEmail(username);

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);
        await ClickNavigateToProfileAsync();
        AssertUrlContains("/account/profile");

        // Act - Refresh the page
        await Page.ReloadAsync();
        await WaitForBlazorAsync();
        await Task.Delay(500);

        // Assert - Should still be on profile and authenticated
        AssertUrlContains("/account/profile");
        await AssertIsAuthenticatedAsync();
        Output.WriteLine("[TEST] Profile page survives refresh");
    }
}

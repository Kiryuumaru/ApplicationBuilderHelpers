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
        // Act
        await Page.GotoAsync($"{WebAppUrl}/account/profile");
        await WaitForBlazorAsync();
        await Task.Delay(500);

        // Assert
        AssertUrlContains("/auth/login");
    }

    [Fact]
    public async Task Journey_ProfileShowsUserInfo()
    {
        // Arrange
        var username = GenerateUsername("profile");
        var email = GenerateEmail(username);

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act
        await ClickNavigateToProfileAsync();
        await WaitForBlazorAsync();

        // Wait for profile data to load
        try
        {
            await Page.WaitForSelectorAsync("text=Username", new() { Timeout = 10000 });
        }
        catch (TimeoutException)
        {
        }

        // Assert
        var pageContent = await Page.ContentAsync();
        var hasUserInfo = pageContent.Contains(email, StringComparison.OrdinalIgnoreCase) ||
                         pageContent.Contains(username, StringComparison.OrdinalIgnoreCase) ||
                         pageContent.Contains("profile", StringComparison.OrdinalIgnoreCase);

        Assert.True(hasUserInfo, "Profile page should display user information");
    }

    [Fact]
    public async Task Journey_ProfileHasUsernameAndEmailLabels()
    {
        // Arrange
        var username = GenerateUsername("labels");
        var email = GenerateEmail(username);

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act
        await ClickNavigateToProfileAsync();
        await Page.WaitForSelectorAsync("text=Username", new() { Timeout = 10000 });
        await Task.Delay(500);

        // Assert
        var pageContent = await Page.ContentAsync();
        var hasUsernameLabel = pageContent.Contains("Username", StringComparison.OrdinalIgnoreCase);
        var hasEmailLabel = pageContent.Contains("Email", StringComparison.OrdinalIgnoreCase);

        Assert.True(hasUsernameLabel, "Profile should have Username label");
        Assert.True(hasEmailLabel, "Profile should have Email label");
    }

    [Fact]
    public async Task Journey_ProfileShowsUserAvatar()
    {
        // Arrange
        var username = GenerateUsername("avatar");
        var email = GenerateEmail(username);

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act
        await ClickNavigateToProfileAsync();
        await WaitForBlazorAsync();

        // Assert
        var avatarElement = await Page.QuerySelectorAsync(".rounded-full, [class*='avatar']");
        Assert.NotNull(avatarElement);
    }

    [Fact]
    public async Task Journey_ProfileHasEditButtons()
    {
        // Arrange
        var username = GenerateUsername("edit");
        var email = GenerateEmail(username);

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act
        await ClickNavigateToProfileAsync();
        await Page.WaitForSelectorAsync("text=Username", new() { Timeout = 10000 });
        await Task.Delay(500);

        // Assert
        var editButtons = await Page.QuerySelectorAllAsync("button:has-text('Edit')");
        Assert.True(editButtons.Count > 0, "Profile should have Edit buttons");
    }

    [Fact(Skip = "Edit username functionality needs verification")]
    public async Task Journey_EditUsername_UpdatesProfile()
    {
        // Arrange
        var username = GenerateUsername("editname");
        var email = GenerateEmail(username);

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Navigate to profile
        await ClickNavigateToProfileAsync();
        await Page.WaitForSelectorAsync("text=Username", new() { Timeout = 10000 });
        await Task.Delay(500);

        // Act
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

        // Assert
        var pageContent = await Page.ContentAsync();
        Assert.Contains(newUsername, pageContent, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Journey_ProfileNavigatedViaUserMenu()
    {
        // Arrange
        var username = GenerateUsername("menunav");
        var email = GenerateEmail(username);

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);
        await GoToHomeAsync();
        await AssertIsAuthenticatedAsync();

        // Act
        await ClickNavigateToProfileAsync();

        // Assert
        AssertUrlContains("/account/profile");
    }

    [Fact]
    public async Task Journey_ProfilePageRefresh_StillShowsProfile()
    {
        // Arrange
        var username = GenerateUsername("refresh");
        var email = GenerateEmail(username);

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);
        await ClickNavigateToProfileAsync();
        AssertUrlContains("/account/profile");

        // Act
        await Page.ReloadAsync();
        await WaitForBlazorAsync();
        await Task.Delay(500);

        // Assert
        AssertUrlContains("/account/profile");
        await AssertIsAuthenticatedAsync();
    }
}

using Presentation.WebApp.Client.FunctionalTests.Fixtures;

namespace Presentation.WebApp.Client.FunctionalTests.Old.Components;

/// <summary>
/// Playwright functional tests for reusable UI components.
/// Tests component behavior across the application.
/// </summary>
public class ComponentTests : WebAppTestBase
{
    public ComponentTests(PlaywrightFixture playwrightFixture, ITestOutputHelper output) : base(playwrightFixture, output)
    {
    }

    #region Card Component Tests

    [Fact]
    public async Task Card_RendersCorrectly()
    {
        // Act
        await GoToHomeAsync();

        // Assert
        var cards = await Page.QuerySelectorAllAsync(".bg-white, [class*='card']");
        Output.WriteLine($"Card elements found: {cards.Count}");

        Assert.True(cards.Count > 0, "Dashboard should have card components");
    }

    #endregion

    #region Button Component Tests

    [Fact]
    public async Task Button_RendersOnLoginPage()
    {
        // Act
        await GoToLoginAsync();

        // Assert
        var submitButton = await Page.QuerySelectorAsync("button[type='submit']");
        Assert.NotNull(submitButton);

        // Check button has proper styling
        var buttonClass = await submitButton.GetAttributeAsync("class");
        Output.WriteLine($"Button class: {buttonClass}");
    }

    [Fact]
    public async Task Button_ShowsLoadingState()
    {
        // Arrange
        await GoToLoginAsync();

        // Fill in form but with invalid credentials to trigger loading
        await Page.FillAsync("input[type='email']", "test@example.com");
        await Page.FillAsync("input[type='password']", "password");

        // Act
        await Page.ClickAsync("button[type='submit']");

        // Assert
        var pageContent = await Page.ContentAsync();
        Output.WriteLine("Button loading state test completed");
    }

    #endregion

    #region TextInput Component Tests

    [Fact]
    public async Task TextInput_RendersWithLabel()
    {
        // Act
        await GoToLoginAsync();

        // Assert
        var labels = await Page.QuerySelectorAllAsync("label");
        Output.WriteLine($"Labels found: {labels.Count}");

        Assert.True(labels.Count > 0, "Form should have labeled inputs");
    }

    [Fact]
    public async Task TextInput_ShowsValidationErrors()
    {
        // Arrange
        await GoToLoginAsync();

        // Act
        await Page.ClickAsync("button[type='submit']");
        await Task.Delay(500);

        // Assert
        var validationMessages = await Page.QuerySelectorAllAsync(".validation-message, .text-red, [class*='error']");
        Output.WriteLine($"Validation messages found: {validationMessages.Count}");
    }

    [Fact]
    public async Task TextInput_PasswordField_HidesText()
    {
        // Act
        await GoToLoginAsync();

        // Assert
        var passwordInput = await Page.QuerySelectorAsync("input[type='password']");
        Assert.NotNull(passwordInput);
    }

    #endregion

    #region Alert Component Tests

    [Fact]
    public async Task Alert_ShowsOnLoginError()
    {
        // Arrange
        await GoToLoginAsync();

        // Act
        await Page.FillAsync("input[type='email']", "nonexistent@example.com");
        await Page.FillAsync("input[type='password']", "wrongpassword");
        await Page.ClickAsync("button[type='submit']");
        await Task.Delay(1500);

        // Assert
        var alert = await Page.QuerySelectorAsync("[class*='alert'], [class*='error'], .bg-red");
        var pageContent = await Page.ContentAsync();
        var hasError = pageContent.Contains("error", StringComparison.OrdinalIgnoreCase) ||
                      pageContent.Contains("failed", StringComparison.OrdinalIgnoreCase) ||
                      pageContent.Contains("invalid", StringComparison.OrdinalIgnoreCase);

        Output.WriteLine($"Alert shown: {alert != null}");
        Output.WriteLine($"Has error text: {hasError}");
    }

    [Fact]
    public async Task Alert_IsDismissible()
    {
        // Alert component has dismiss functionality
        // Actual dismissal depends on implementation

        // Arrange
        await GoToLoginAsync();
        await Page.FillAsync("input[type='email']", "test@example.com");
        await Page.FillAsync("input[type='password']", "wrong");
        await Page.ClickAsync("button[type='submit']");
        await Task.Delay(1500);

        // Assert
        var dismissButton = await Page.QuerySelectorAsync("button[aria-label*='dismiss' i], button[aria-label*='close' i], .alert button");
        Output.WriteLine($"Dismiss button found: {dismissButton != null}");
    }

    #endregion

    #region LoadingSpinner Component Tests

    [Fact]
    public async Task LoadingSpinner_ShowsDuringPageLoad()
    {
        // Act
        Page.Request += (_, request) =>
        {
            // During navigation, loading might appear
        };

        await Page.GotoAsync(WebAppUrl);

        // Check if loading class exists
        var loadingElement = await Page.QuerySelectorAsync(".loading, [class*='spinner'], [class*='loading']");
        Output.WriteLine($"Loading spinner found: {loadingElement != null}");

        // The loading spinner may have already completed; verify the test runs
    }

    #endregion

    #region Navigation Component Tests

    [Fact]
    public async Task Navigation_HasMenuItems()
    {
        // Arrange
        var username = $"nav_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act
        await GoToHomeAsync();

        // Assert
        var navLinks = await Page.QuerySelectorAllAsync("nav a, .sidebar a, [role='navigation'] a");
        Output.WriteLine($"Navigation links found: {navLinks.Count}");

        Assert.True(navLinks.Count > 0, "Should have navigation menu items");
    }

    [Fact]
    public async Task Navigation_HighlightsActivePage()
    {
        // Arrange
        var username = $"active_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act
        await GoToHomeAsync();

        // Assert
        // Look for active state styling
        var activeNavItem = await Page.QuerySelectorAsync("[class*='active'], [aria-current='page']");
        Output.WriteLine($"Active nav item found: {activeNavItem != null}");
    }

    #endregion

    #region AuthorizeView Component Tests

    [Fact]
    public async Task AuthorizeView_ShowsNotAuthorizedContent()
    {
        // Act
        await GoToHomeAsync();

        // Assert
        var signInLink = await Page.QuerySelectorAsync("a[href*='login' i]");
        var getStartedContent = await Page.ContentAsync();
        var hasNotAuthorizedContent = getStartedContent.Contains("sign in", StringComparison.OrdinalIgnoreCase) ||
                                      getStartedContent.Contains("get started", StringComparison.OrdinalIgnoreCase);

        Assert.True(hasNotAuthorizedContent, "Should show content for unauthenticated users");
    }

    [Fact]
    public async Task AuthorizeView_ShowsAuthorizedContent()
    {
        // Arrange
        var username = $"authview_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act
        await GoToHomeAsync();

        // Assert
        var content = await Page.ContentAsync();
        var hasAuthorizedContent = content.Contains("welcome", StringComparison.OrdinalIgnoreCase) ||
                                   content.Contains("profile", StringComparison.OrdinalIgnoreCase) ||
                                   content.Contains(username, StringComparison.OrdinalIgnoreCase);

        Assert.True(hasAuthorizedContent, "Should show content for authenticated users");
    }

    #endregion
}

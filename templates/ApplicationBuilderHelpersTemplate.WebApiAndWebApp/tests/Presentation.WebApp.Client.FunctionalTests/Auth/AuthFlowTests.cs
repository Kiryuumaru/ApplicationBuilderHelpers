using Presentation.WebApp.Client.FunctionalTests.Fixtures;

namespace Presentation.WebApp.Client.FunctionalTests.Auth;

/// <summary>
/// Core authentication flow tests using ONLY UI interactions.
/// These tests simulate real user behavior: click buttons, type in fields.
/// NO direct API calls, NO header manipulation.
/// </summary>
public class AuthFlowTests : WebAppTestBase
{
    public AuthFlowTests(PlaywrightFixture playwrightFixture, ITestOutputHelper output) : base(playwrightFixture, output)
    {
    }

    /// <summary>
    /// THE CRITICAL TEST: Register → Login → Navigate to Profile → See profile data
    /// This is the exact flow that was broken: after login, profile page returned 401.
    /// </summary>
    [Fact]
    public async Task Journey_RegisterLoginNavigateToProfile_SeesProfileData()
    {
        // Arrange
        var username = GenerateUsername("profile");
        var email = GenerateEmail(username);

        // Act 1: Register via UI (fill form, click submit)
        Output.WriteLine("=== STEP 1: Register ===");
        var registered = await RegisterUserAsync(username, email, TestPassword);
        Assert.True(registered, "Registration should succeed");

        // After registration, user should be auto-logged in and on home page
        Output.WriteLine($"After registration, URL: {Page.Url}");
        AssertUrlDoesNotContain("/auth/register");
        AssertUrlDoesNotContain("/auth/login");

        // Act 2: Click to navigate to Profile page (not direct URL navigation)
        Output.WriteLine("=== STEP 2: Navigate to Profile via Click ===");
        await ClickNavigateToProfileAsync();

        // Assert: Should be on profile page
        Output.WriteLine($"After profile click, URL: {Page.Url}");
        AssertUrlContains("/account/profile");

        // Wait for profile page to finish loading
        Output.WriteLine("=== STEP 3: Waiting for Profile Data to Load ===");
        
        // Wait for the profile heading (page structure loaded)
        await WaitForAsync("h2:has-text('Profile Information')");
        Output.WriteLine("[TEST] Profile heading found");

        // Wait for the username label (proves API data loaded successfully)
        await WaitForAsync("label:has-text('Username')");
        Output.WriteLine("[TEST] Username label found - profile data loaded");

        // Assert: Username value matches what we registered
        var usernameValue = await WaitForAsync("label:has-text('Username') + div p, label:has-text('Username') ~ p");
        var displayedUsername = await usernameValue.First.TextContentAsync();
        Output.WriteLine($"[TEST] Displayed username: '{displayedUsername}'");
        Assert.False(string.IsNullOrWhiteSpace(displayedUsername), "Username value should not be empty");
        Assert.Contains(username, displayedUsername!, StringComparison.OrdinalIgnoreCase);

        // Assert: Email value matches what we registered
        var emailValue = await WaitForAsync("label:has-text('Email') + div p, label:has-text('Email') ~ p");
        var displayedEmail = await emailValue.First.TextContentAsync();
        Output.WriteLine($"[TEST] Displayed email: '{displayedEmail}'");
        Assert.False(string.IsNullOrWhiteSpace(displayedEmail), "Email value should not be empty");
        Assert.Contains(email, displayedEmail!, StringComparison.OrdinalIgnoreCase);

        // Assert: User ID label is visible (proves profile data loaded from API)
        await WaitForAsync("label:has-text('User ID')");

        Output.WriteLine($"[SUCCESS] Profile page displays username '{displayedUsername}' and email '{displayedEmail}'");
    }

    /// <summary>
    /// Test that session persists after page refresh.
    /// </summary>
    [Fact]
    public async Task Journey_LoginRefreshPage_StillLoggedIn()
    {
        // Arrange
        var username = GenerateUsername("refresh");
        var email = GenerateEmail(username);

        // Register and login
        await RegisterUserAsync(username, email, TestPassword);
        
        // Verify we're logged in
        await AssertIsAuthenticatedAsync();
        var urlBeforeRefresh = Page.Url;
        Output.WriteLine($"Before refresh, URL: {urlBeforeRefresh}");

        // Act: Refresh the page
        Output.WriteLine("=== Refreshing page ===");
        await Page.ReloadAsync();
        await WaitForBlazorAsync();

        // Assert: Should still be logged in, not redirected to login
        Output.WriteLine($"After refresh, URL: {Page.Url}");
        
        // Should NOT be on login page
        AssertUrlDoesNotContain("/auth/login");
        
        // Should still see authenticated UI
        await AssertIsAuthenticatedAsync();
    }

    /// <summary>
    /// Test login with existing user (separate from registration).
    /// </summary>
    [Fact]
    public async Task Journey_LoginExistingUser_Success()
    {
        // Arrange: Register a user first
        var username = GenerateUsername("login");
        var email = GenerateEmail(username);
        await RegisterUserAsync(username, email, TestPassword);
        
        // Logout to test login flow separately
        await LogoutAsync();
        await AssertIsNotAuthenticatedAsync();

        // Act: Login via UI
        Output.WriteLine("=== Logging in ===");
        var loggedIn = await LoginAsync(email, TestPassword);

        // Assert
        Assert.True(loggedIn, "Login should succeed");
        AssertUrlDoesNotContain("/auth/login");
        await AssertIsAuthenticatedAsync();
    }

    /// <summary>
    /// Test logout flow.
    /// </summary>
    [Fact]
    public async Task Journey_Logout_RedirectsToLogin()
    {
        // Arrange: Register and login
        var username = GenerateUsername("logout");
        var email = GenerateEmail(username);
        await RegisterUserAsync(username, email, TestPassword);
        await AssertIsAuthenticatedAsync();

        // Act: Logout
        Output.WriteLine("=== Logging out ===");
        await LogoutAsync();

        // Assert: Should be on login page and not authenticated
        AssertUrlContains("/auth/login");
        await AssertIsNotAuthenticatedAsync();
    }

    /// <summary>
    /// Test full cycle: login → logout → login again.
    /// </summary>
    [Fact]
    public async Task Journey_LoginLogoutLoginAgain_Works()
    {
        // Arrange
        var username = GenerateUsername("cycle");
        var email = GenerateEmail(username);

        // Register
        await RegisterUserAsync(username, email, TestPassword);
        await AssertIsAuthenticatedAsync();
        Output.WriteLine("Step 1: Registered and authenticated");

        // Logout
        await LogoutAsync();
        await AssertIsNotAuthenticatedAsync();
        Output.WriteLine("Step 2: Logged out");

        // Login again
        var loggedIn = await LoginAsync(email, TestPassword);
        Assert.True(loggedIn, "Second login should succeed");
        await AssertIsAuthenticatedAsync();
        Output.WriteLine("Step 3: Logged in again");

        // Navigate to profile to verify full auth works
        await ClickNavigateToProfileAsync();
        AssertUrlContains("/account/profile");
        Output.WriteLine("Step 4: Navigated to profile successfully");
    }

    /// <summary>
    /// Test password change cycle: change password → logout → old password fails → new password works.
    /// </summary>
    [Fact(Skip = "Change Password UI not yet implemented")]
    public async Task Journey_ChangePassword_OldFailsNewWorks()
    {
        // Arrange
        var username = GenerateUsername("pwchange");
        var email = GenerateEmail(username);
        var newPassword = "NewPassword456!";

        // Register
        await RegisterUserAsync(username, email, TestPassword);
        await AssertIsAuthenticatedAsync();
        Output.WriteLine("Step 1: Registered");

        // Change password
        var changed = await ChangePasswordAsync(TestPassword, newPassword);
        Assert.True(changed, "Password change should succeed");
        Output.WriteLine("Step 2: Password changed");

        // Logout
        await LogoutAsync();
        Output.WriteLine("Step 3: Logged out");

        // Try login with OLD password - should fail
        await GoToLoginAsync();
        await Page.Locator("#email").FillAsync(email);
        await Page.Locator("#password").FillAsync(TestPassword);
        await Page.Locator("button[type='submit']").ClickAsync();
        
        // Should still be on login page (failed)
        await Task.Delay(2000); // Wait for response
        var stillOnLogin = Page.Url.Contains("/auth/login", StringComparison.OrdinalIgnoreCase);
        Assert.True(stillOnLogin, "Old password should NOT work after change");
        Output.WriteLine("Step 4: Old password rejected (correct)");

        // Try login with NEW password - should succeed
        await Page.Locator("#password").FillAsync(""); // Clear
        await Page.Locator("#password").FillAsync(newPassword);
        await Page.Locator("button[type='submit']").ClickAsync();
        
        var success = await WaitForUrlNotContainsAsync("/auth/login", timeoutMs: 10000);
        Assert.True(success, "New password should work");
        await AssertIsAuthenticatedAsync();
        Output.WriteLine("Step 5: New password works (correct)");
    }

    /// <summary>
    /// Test login with wrong password shows error.
    /// </summary>
    [Fact]
    public async Task Login_WrongPassword_ShowsError()
    {
        // Arrange
        var username = GenerateUsername("wrongpw");
        var email = GenerateEmail(username);
        await RegisterUserAsync(username, email, TestPassword);
        await LogoutAsync();

        // Act: Try to login with wrong password
        await GoToLoginAsync();
        await Page.Locator("#email").FillAsync(email);
        await Page.Locator("#password").FillAsync("WrongPassword123!");
        await Page.Locator("button[type='submit']").ClickAsync();

        // Wait a bit for response
        await Task.Delay(2000);

        // Assert: Should still be on login page
        AssertUrlContains("/auth/login");
        
        // Should see some kind of error indication
        var hasError = await WaitForErrorMessageAsync(timeoutMs: 3000);
        var pageContent = await Page.ContentAsync();
        var hasErrorText = pageContent.Contains("invalid", StringComparison.OrdinalIgnoreCase) ||
                           pageContent.Contains("error", StringComparison.OrdinalIgnoreCase) ||
                           pageContent.Contains("incorrect", StringComparison.OrdinalIgnoreCase) ||
                           pageContent.Contains("failed", StringComparison.OrdinalIgnoreCase);

        Assert.True(hasError || hasErrorText, "Should show error for wrong password");
    }
}

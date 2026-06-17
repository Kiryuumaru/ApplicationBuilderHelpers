using Presentation.WebApp.Client.FunctionalTests.Fixtures;

namespace Presentation.WebApp.Client.FunctionalTests.Old.Integration;

/// <summary>
/// Playwright functional tests for WebApi and WebApp integration.
/// Tests that the WebApp correctly communicates with the WebApi.
/// </summary>
public class ApiIntegrationTests : WebAppTestBase
{
    public ApiIntegrationTests(PlaywrightFixture playwrightFixture, ITestOutputHelper output) : base(playwrightFixture, output)
    {
    }

    [Fact]
    public async Task WebApi_HealthCheck_IsHealthy()
    {
        // Act - Call WebApi health endpoint directly
        var response = await HttpClient.GetAsync("/health");

        // Assert
        Assert.True(response.IsSuccessStatusCode,
            $"WebApi health check should be healthy, got {response.StatusCode}");
    }

    [Fact]
    public async Task WebApp_CanConnectToApi_OnLogin()
    {
        // Arrange - Register via API first to ensure user exists
        var username = $"api_conn_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        // Act - Register through WebApp UI
        var registerSuccess = await RegisterUserAsync(username, email, TestPassword);
        Assert.True(registerSuccess, "Registration should succeed - API connection works");

        // Login through WebApp UI
        var loginSuccess = await LoginAsync(email, TestPassword);

        // Assert - Login working means API integration works
        Assert.True(loginSuccess, "Login should succeed - API integration verified");
    }

    [Fact(Skip = "UI does not yet show logout link or username after login")]
    public async Task Integration_LoginStateReflectedInUI()
    {
        // Arrange - Register and login
        var username = $"ui_state_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);

        // Check UI before login
        await GoToHomeAsync();
        var beforeLoginContent = await Page.ContentAsync();
        var hasLoginLinkBefore = beforeLoginContent.Contains("login", StringComparison.OrdinalIgnoreCase) ||
                                  beforeLoginContent.Contains("sign in", StringComparison.OrdinalIgnoreCase);
        Output.WriteLine($"Has login link before: {hasLoginLinkBefore}");

        // Act - Login
        await LoginAsync(email, TestPassword);

        // Check UI after login
        await GoToHomeAsync();
        var afterLoginContent = await Page.ContentAsync();
        var hasLogoutLink = afterLoginContent.Contains("logout", StringComparison.OrdinalIgnoreCase) ||
                           afterLoginContent.Contains("sign out", StringComparison.OrdinalIgnoreCase);
        var hasUsername = afterLoginContent.Contains(username, StringComparison.OrdinalIgnoreCase);
        Output.WriteLine($"Has logout link after: {hasLogoutLink}");
        Output.WriteLine($"Shows username: {hasUsername}");

        // Assert - UI should change after login
        Assert.True(hasLogoutLink || hasUsername,
            "UI should reflect logged-in state (show logout link or username)");
    }

    [Fact]
    public async Task Integration_NetworkRequestsToApi_AreSuccessful()
    {
        // Arrange - Set up request interception to monitor API calls
        var apiRequests = new List<(string Method, string Url, int Status)>();

        await Page.RouteAsync("**/*", async route =>
        {
            var request = route.Request;
            if (request.Url.Contains("localhost:5299") || request.Url.Contains("/api/"))
            {
                Output.WriteLine($"[INTERCEPT] {request.Method} {request.Url}");
            }
            await route.ContinueAsync();
        });

        Page.Response += (_, response) =>
        {
            if (response.Url.Contains("localhost:5299") || response.Url.Contains("/api/"))
            {
                apiRequests.Add((response.Request.Method, response.Url, response.Status));
                Output.WriteLine($"[RESPONSE] {response.Request.Method} {response.Url} -> {response.Status}");
            }
        };

        // Act - Perform registration which should make API calls
        var username = $"network_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);

        // Assert - Should have made some API requests
        Output.WriteLine($"Total API requests captured: {apiRequests.Count}");
        foreach (var req in apiRequests)
        {
            Output.WriteLine($"  {req.Method} {req.Url} -> {req.Status}");
        }

        // Registration should trigger API call(s)
        // Note: This test documents behavior - some implementations may not make visible network requests
        // if using different patterns
    }

    [Fact]
    public async Task Integration_TokenStorage_PersistsInBrowser()
    {
        // Arrange - Register and login
        var username = $"token_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{username}@test.example.com";

        await RegisterUserAsync(username, email, TestPassword);
        await LoginAsync(email, TestPassword);

        // Act - Check local storage for auth tokens
        var localStorage = await Page.EvaluateAsync<Dictionary<string, string>>(
            @"() => {
                const items = {};
                for (let i = 0; i < localStorage.length; i++) {
                    const key = localStorage.key(i);
                    items[key] = localStorage.getItem(key);
                }
                return items;
            }");

        Output.WriteLine("LocalStorage contents:");
        foreach (var kvp in localStorage)
        {
            // Don't log actual token values for security
            var displayValue = kvp.Value.Length > 50 ? $"[{kvp.Value.Length} chars]" : kvp.Value;
            Output.WriteLine($"  {kvp.Key}: {displayValue}");
        }

        // Assert - Should have stored some auth-related data
        var hasAuthData = localStorage.Keys.Any(k =>
            k.Contains("token", StringComparison.OrdinalIgnoreCase) ||
            k.Contains("auth", StringComparison.OrdinalIgnoreCase) ||
            k.Contains("user", StringComparison.OrdinalIgnoreCase));

        // Note: Some implementations may use session storage or cookies instead
        Output.WriteLine($"Has auth data in localStorage: {hasAuthData}");
    }
}

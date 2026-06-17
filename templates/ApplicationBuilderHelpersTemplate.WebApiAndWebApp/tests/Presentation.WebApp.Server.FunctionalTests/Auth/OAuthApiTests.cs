using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
namespace Presentation.WebApp.Server.FunctionalTests.Auth;

/// <summary>
/// Functional tests for OAuth/External Login API endpoints.
/// Tests external login providers, linking, and unlinking.
/// </summary>
public class OAuthApiTests : WebAppTestBase
{
    public OAuthApiTests(ITestOutputHelper output) : base(output)
    {
    }

    #region Get Providers Tests

    [TimedFact]
    public async Task GetProviders_ReturnsAvailableProviders()
    {
        Output.WriteLine("[TEST] GetProviders_ReturnsAvailableProviders");

        Output.WriteLine("[STEP] GET /api/v1/auth/external/providers...");
        var response = await HttpClient.GetAsync("/api/v1/auth/external/providers");

        Output.WriteLine($"[RECEIVED] Status: {(int)response.StatusCode} {response.StatusCode}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Output.WriteLine($"[RECEIVED] Body: {content}");

        var result = JsonSerializer.Deserialize<OAuthProvidersResponse>(content, JsonOptions);
        Assert.NotNull(result);
        Assert.NotEmpty(result!.Providers);

        // Mock provider should be enabled by default
        var mockProvider = result.Providers.FirstOrDefault(p => p.Provider.Equals("mock", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(mockProvider);
        Assert.True(mockProvider!.IsEnabled, "Mock provider should be enabled by default");

        Output.WriteLine("[PASS] Get providers returned available providers with Mock enabled");
    }

    [TimedFact]
    public async Task GetProviders_NoAuthRequired()
    {
        Output.WriteLine("[TEST] GetProviders_NoAuthRequired");

        // This endpoint should be accessible without authentication
        Output.WriteLine("[STEP] GET /api/v1/auth/external/providers without token...");
        var response = await HttpClient.GetAsync("/api/v1/auth/external/providers");

        Output.WriteLine($"[RECEIVED] Status: {(int)response.StatusCode} {response.StatusCode}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Output.WriteLine("[PASS] Get providers accessible without authentication");
    }

    #endregion

    #region Initiate OAuth Tests

    [TimedFact]
    public async Task InitiateOAuth_WithMockProvider_ReturnsAuthorizationUrl()
    {
        Output.WriteLine("[TEST] InitiateOAuth_WithMockProvider_ReturnsAuthorizationUrl");

        var request = new OAuthLoginRequest("mock", "https://localhost/callback");

        Output.WriteLine("[STEP] POST /api/v1/auth/external/mock...");
        var response = await HttpClient.PostAsJsonAsync("/api/v1/auth/external/mock", request);

        Output.WriteLine($"[RECEIVED] Status: {(int)response.StatusCode} {response.StatusCode}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Output.WriteLine($"[RECEIVED] Body: {content}");

        var result = JsonSerializer.Deserialize<OAuthAuthorizationResponse>(content, JsonOptions);
        Assert.NotNull(result);
        Assert.NotEmpty(result!.AuthorizationUrl);
        Assert.NotEmpty(result.State);

        Output.WriteLine("[PASS] Initiate OAuth returned authorization URL with state");
    }

    [TimedFact]
    public async Task InitiateOAuth_WithInvalidProvider_Returns400()
    {
        Output.WriteLine("[TEST] InitiateOAuth_WithInvalidProvider_Returns400");

        var request = new OAuthLoginRequest("invalidprovider", "https://localhost/callback");

        Output.WriteLine("[STEP] POST /api/v1/auth/external/invalidprovider...");
        var response = await HttpClient.PostAsJsonAsync("/api/v1/auth/external/invalidprovider", request);

        Output.WriteLine($"[RECEIVED] Status: {(int)response.StatusCode} {response.StatusCode}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Output.WriteLine("[PASS] Returns 400 for invalid provider");
    }

    [TimedFact]
    public async Task InitiateOAuth_WithDisabledProvider_Returns400()
    {
        Output.WriteLine("[TEST] InitiateOAuth_WithDisabledProvider_Returns400");

        var request = new OAuthLoginRequest("google", "https://localhost/callback");

        // Google is not configured, so it should be disabled
        Output.WriteLine("[STEP] POST /api/v1/auth/external/google...");
        var response = await HttpClient.PostAsJsonAsync("/api/v1/auth/external/google", request);

        Output.WriteLine($"[RECEIVED] Status: {(int)response.StatusCode} {response.StatusCode}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Output.WriteLine("[PASS] Returns 400 for disabled provider");
    }

    #endregion

    #region OAuth Callback Tests

    [TimedFact]
    public async Task OAuthCallback_WithMockProvider_CreatesNewUserAndSession()
    {
        Output.WriteLine("[TEST] OAuthCallback_WithMockProvider_CreatesNewUserAndSession");

        // First, initiate OAuth to get a state
        var initiateRequest = new OAuthLoginRequest("mock", "https://localhost/callback");
        var initiateResponse = await HttpClient.PostAsJsonAsync("/api/v1/auth/external/mock", initiateRequest);
        Assert.Equal(HttpStatusCode.OK, initiateResponse.StatusCode);
        var initiateContent = await initiateResponse.Content.ReadAsStringAsync();
        var initiateResult = JsonSerializer.Deserialize<OAuthAuthorizationResponse>(initiateContent, JsonOptions);
        Assert.NotNull(initiateResult);

        // Now callback with mock code and state
        var callbackRequest = new OAuthCallbackRequest(
            Provider: "mock",
            Code: "mock_auth_code",
            State: initiateResult!.State,
            RedirectUri: "https://localhost/callback");

        Output.WriteLine("[STEP] POST /api/v1/auth/external/callback...");
        var response = await HttpClient.PostAsJsonAsync("/api/v1/auth/external/callback", callbackRequest);

        Output.WriteLine($"[RECEIVED] Status: {(int)response.StatusCode} {response.StatusCode}");

        // Should return 201 Created with tokens for new user
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Output.WriteLine($"[RECEIVED] Body: {content}");

        var result = JsonSerializer.Deserialize<AuthResponse>(content, JsonOptions);
        Assert.NotNull(result);
        Assert.NotEmpty(result!.AccessToken);
        Assert.NotEmpty(result.RefreshToken);
        Assert.NotNull(result.User);

        Output.WriteLine("[PASS] OAuth callback created new user with session");
    }

    [TimedFact]
    public async Task OAuthCallback_WithEmptyState_Returns400()
    {
        Output.WriteLine("[TEST] OAuthCallback_WithEmptyState_Returns400");

        var callbackRequest = new OAuthCallbackRequest(
            Provider: "mock",
            Code: "mock_auth_code",
            State: "", // Empty state should fail validation
            RedirectUri: "https://localhost/callback");

        Output.WriteLine("[STEP] POST /api/v1/auth/external/callback with empty state...");
        var response = await HttpClient.PostAsJsonAsync("/api/v1/auth/external/callback", callbackRequest);

        Output.WriteLine($"[RECEIVED] Status: {(int)response.StatusCode} {response.StatusCode}");

        // Empty state should fail validation (400 or 401)
        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.Unauthorized,
            $"Expected 400 or 401, got {response.StatusCode}");
        Output.WriteLine("[PASS] Returns error for empty state");
    }

    #endregion

    #region Get External Logins Tests

    [TimedFact]
    public async Task GetExternalLogins_WithoutAuth_Returns401()
    {
        Output.WriteLine("[TEST] GetExternalLogins_WithoutAuth_Returns401");

        var randomUserId = Guid.NewGuid();
        Output.WriteLine($"[STEP] GET /api/v1/auth/users/{randomUserId}/identity/external without token...");
        var response = await HttpClient.GetAsync($"/api/v1/auth/users/{randomUserId}/identity/external");

        Output.WriteLine($"[RECEIVED] Status: {(int)response.StatusCode} {response.StatusCode}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Output.WriteLine("[PASS] Returns 401 without authentication");
    }

    [TimedFact]
    public async Task GetExternalLogins_WithAuth_ReturnsEmptyListForNewUser()
    {
        Output.WriteLine("[TEST] GetExternalLogins_WithAuth_ReturnsEmptyListForNewUser");

        // Register a new user with password (no external logins)
        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);

        var userId = authResult!.User.Id;
        Output.WriteLine($"[STEP] GET /api/v1/auth/users/{userId}/identity/external...");
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/auth/users/{userId}/identity/external");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
        var response = await HttpClient.SendAsync(request);

        Output.WriteLine($"[RECEIVED] Status: {(int)response.StatusCode} {response.StatusCode}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Output.WriteLine($"[RECEIVED] Body: {content}");

        var result = JsonSerializer.Deserialize<ExternalLoginsResponse>(content, JsonOptions);
        Assert.NotNull(result);
        Assert.Empty(result!.Logins);

        Output.WriteLine("[PASS] Returns empty list for user with no external logins");
    }

    #endregion

    #region Unlink External Login Tests

    [TimedFact]
    public async Task UnlinkExternalLogin_WithoutAuth_Returns401()
    {
        Output.WriteLine("[TEST] UnlinkExternalLogin_WithoutAuth_Returns401");

        var randomUserId = Guid.NewGuid();
        Output.WriteLine($"[STEP] DELETE /api/v1/auth/users/{randomUserId}/identity/external/mock without token...");
        var response = await HttpClient.DeleteAsync($"/api/v1/auth/users/{randomUserId}/identity/external/mock");

        Output.WriteLine($"[RECEIVED] Status: {(int)response.StatusCode} {response.StatusCode}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Output.WriteLine("[PASS] Returns 401 without authentication");
    }

    [TimedFact]
    public async Task UnlinkExternalLogin_NonExistentProvider_Returns404()
    {
        Output.WriteLine("[TEST] UnlinkExternalLogin_NonExistentProvider_Returns404");

        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);

        var userId = authResult!.User.Id;
        Output.WriteLine($"[STEP] DELETE /api/v1/auth/users/{userId}/identity/external/mock (user has no external logins)...");
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/auth/users/{userId}/identity/external/mock");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
        var response = await HttpClient.SendAsync(request);

        Output.WriteLine($"[RECEIVED] Status: {(int)response.StatusCode} {response.StatusCode}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Output.WriteLine("[PASS] Returns 404 for non-existent external login");
    }

    #endregion

    #region Helper Methods

    private async Task<AuthResponse?> RegisterUniqueUserAsync()
    {
        var username = $"oauthtest_{Guid.NewGuid():N}";
        return await RegisterUserAsync(username);
    }

    private async Task<AuthResponse?> RegisterUserAsync(string username)
    {
        var registerRequest = new
        {
            Username = username,
            Email = $"{username}@example.com",
            Password = TestPassword,
            ConfirmPassword = TestPassword
        };
        var response = await HttpClient.PostAsJsonAsync("/api/v1/auth/register", registerRequest);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            Output.WriteLine($"[ERROR] Registration failed: {errorContent}");
            return null;
        }

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<AuthResponse>(content, JsonOptions);
    }

    #endregion

    #region Response DTOs

    private record OAuthProvidersResponse(IReadOnlyList<OAuthProviderInfo> Providers);

    private record OAuthProviderInfo(string Provider, string DisplayName, bool IsEnabled, string? IconName);

    private record OAuthLoginRequest(string Provider, string RedirectUri);

    private record OAuthAuthorizationResponse(string AuthorizationUrl, string State);

    private record OAuthCallbackRequest(string Provider, string Code, string State, string RedirectUri);

    private record AuthResponse(
        string AccessToken,
        string RefreshToken,
        string TokenType,
        int ExpiresIn,
        UserInfoResponse User);

    private record UserInfoResponse(
        Guid Id,
        string Username,
        string? Email,
        IReadOnlyCollection<string> Roles,
        IReadOnlyCollection<string> Permissions);

    private record ExternalLoginsResponse(IReadOnlyList<ExternalLoginInfo> Logins);

    private record ExternalLoginInfo(string Provider, string? DisplayName, string? Email, DateTimeOffset LinkedAt);

    #endregion
}





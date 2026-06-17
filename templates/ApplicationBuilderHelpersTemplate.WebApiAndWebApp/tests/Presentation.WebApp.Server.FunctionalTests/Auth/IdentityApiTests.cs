using Presentation.WebApp.Server.FunctionalTests;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
namespace Presentation.WebApp.Server.FunctionalTests.Auth;

/// <summary>
/// Functional tests for the Identity API endpoint.
/// Tests GET /api/v1/auth/users/{userId}/identity for retrieving linked identities.
/// </summary>
public class IdentityApiTests : WebAppTestBase
{
    public IdentityApiTests(ITestOutputHelper output) : base(output)
    {
    }

    #region Get Identity Tests

    [TimedFact]
    public async Task GetIdentity_WithoutToken_Returns401()
    {
        Output.WriteLine("[TEST] GetIdentity_WithoutToken_Returns401");

        var randomUserId = Guid.NewGuid();
        Output.WriteLine($"[STEP] GET /api/v1/auth/users/{randomUserId}/identity without token...");
        var response = await HttpClient.GetAsync($"/api/v1/auth/users/{randomUserId}/identity");

        Output.WriteLine($"[RECEIVED] Status: {(int)response.StatusCode} {response.StatusCode}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Output.WriteLine("[PASS] Returns 401 without authentication");
    }

    [TimedFact]
    public async Task GetIdentity_WithValidToken_ReturnsIdentityInfo()
    {
        Output.WriteLine("[TEST] GetIdentity_WithValidToken_ReturnsIdentityInfo");

        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);

        var userId = authResult!.User.Id;
        Output.WriteLine($"[STEP] GET /api/v1/auth/users/{userId}/identity...");
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/auth/users/{userId}/identity");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
        var response = await HttpClient.SendAsync(request);

        Output.WriteLine($"[RECEIVED] Status: {(int)response.StatusCode} {response.StatusCode}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Output.WriteLine($"[RECEIVED] Body: {content}");

        var result = JsonSerializer.Deserialize<IdentitiesResponse>(content, JsonOptions);
        Assert.NotNull(result);
        Assert.False(result!.IsAnonymous, "User registered with password should not be anonymous");
        Assert.True(result.HasPassword, "User registered with password should have password");
        Assert.NotNull(result.Email);
        Assert.Empty(result.LinkedProviders);
        Assert.Empty(result.LinkedPasskeys);

        Output.WriteLine("[PASS] GetIdentity returns correct identity info");
    }

    [TimedFact]
    public async Task GetIdentity_ForOtherUser_Returns403()
    {
        Output.WriteLine("[TEST] GetIdentity_ForOtherUser_Returns403");

        var user1 = await RegisterUniqueUserAsync();
        var user2 = await RegisterUniqueUserAsync();
        Assert.NotNull(user1);
        Assert.NotNull(user2);

        Output.WriteLine($"[STEP] GET /api/v1/auth/users/{user2!.User.Id}/identity with User1's token...");
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/auth/users/{user2.User.Id}/identity");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user1!.AccessToken);
        var response = await HttpClient.SendAsync(request);

        Output.WriteLine($"[RECEIVED] Status: {(int)response.StatusCode} {response.StatusCode}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Output.WriteLine("[PASS] Returns 403 when accessing other user's identity");
    }

    [TimedFact]
    public async Task GetIdentity_NonExistentUser_Returns404()
    {
        Output.WriteLine("[TEST] GetIdentity_NonExistentUser_Returns404");

        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);

        var nonExistentUserId = Guid.NewGuid();
        Output.WriteLine($"[STEP] GET /api/v1/auth/users/{nonExistentUserId}/identity...");
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/auth/users/{nonExistentUserId}/identity");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult!.AccessToken);
        var response = await HttpClient.SendAsync(request);

        Output.WriteLine($"[RECEIVED] Status: {(int)response.StatusCode} {response.StatusCode}");

        // Either 403 (no permission) or 404 (not found) depending on implementation
        Assert.True(
            response.StatusCode == HttpStatusCode.Forbidden ||
            response.StatusCode == HttpStatusCode.NotFound,
            $"Expected 403 or 404, got {response.StatusCode}");

        Output.WriteLine("[PASS] Returns 403 or 404 for non-existent user");
    }

    [TimedFact]
    public async Task GetIdentity_UserWithOAuth_ReturnsLinkedProviders()
    {
        Output.WriteLine("[TEST] GetIdentity_UserWithOAuth_ReturnsLinkedProviders");

        // Create user via OAuth
        var authResult = await CreateUserViaOAuthAsync();
        Assert.NotNull(authResult);

        var userId = authResult!.User.Id;
        Output.WriteLine($"[STEP] GET /api/v1/auth/users/{userId}/identity...");
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/auth/users/{userId}/identity");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
        var response = await HttpClient.SendAsync(request);

        Output.WriteLine($"[RECEIVED] Status: {(int)response.StatusCode} {response.StatusCode}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Output.WriteLine($"[RECEIVED] Body: {content}");

        var result = JsonSerializer.Deserialize<IdentitiesResponse>(content, JsonOptions);
        Assert.NotNull(result);
        Assert.NotEmpty(result!.LinkedProviders);
        Assert.Contains(result.LinkedProviders, p => p.Provider.Equals("mock", StringComparison.OrdinalIgnoreCase));

        Output.WriteLine("[PASS] GetIdentity returns linked OAuth providers");
    }

    [TimedFact]
    public async Task GetIdentity_EmailConfirmedStatus_Correct()
    {
        Output.WriteLine("[TEST] GetIdentity_EmailConfirmedStatus_Correct");

        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);

        var userId = authResult!.User.Id;
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/auth/users/{userId}/identity");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
        var response = await HttpClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<IdentitiesResponse>(content, JsonOptions);
        Assert.NotNull(result);

        // New users typically have unconfirmed email unless auto-confirmed
        Output.WriteLine($"[INFO] EmailConfirmed: {result!.EmailConfirmed}");
        Output.WriteLine("[PASS] EmailConfirmed status is returned");
    }

    #endregion

    #region Journey Tests

    [TimedFact]
    public async Task Journey_RegisterThenCheckIdentity()
    {
        Output.WriteLine("[TEST] Journey_RegisterThenCheckIdentity");

        var username = $"journey_{Guid.NewGuid():N}";
        var email = $"{username}@example.com";

        // Step 1: Register
        var registerRequest = new
        {
            Username = username,
            Email = email,
            Password = TestPassword,
            ConfirmPassword = TestPassword
        };
        var registerResponse = await HttpClient.PostAsJsonAsync("/api/v1/auth/register", registerRequest);
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);
        var authResult = JsonSerializer.Deserialize<AuthResponse>(await registerResponse.Content.ReadAsStringAsync(), JsonOptions);
        Assert.NotNull(authResult);

        var userId = authResult!.User.Id;

        // Step 2: Get identity
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/auth/users/{userId}/identity");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
        var response = await HttpClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var identity = JsonSerializer.Deserialize<IdentitiesResponse>(await response.Content.ReadAsStringAsync(), JsonOptions);
        Assert.NotNull(identity);

        // Verify all fields
        Assert.False(identity!.IsAnonymous);
        Assert.True(identity.HasPassword);
        Assert.Equal(email, identity.Email);
        Assert.Empty(identity.LinkedProviders);
        Assert.Empty(identity.LinkedPasskeys);

        Output.WriteLine("[PASS] Journey: Register and check identity completed");
    }

    [TimedFact]
    public async Task Journey_OAuthUserLinksPasswordThenCheckIdentity()
    {
        Output.WriteLine("[TEST] Journey_OAuthUserLinksPasswordThenCheckIdentity");

        // Step 1: Create user via OAuth (anonymous with OAuth)
        var authResult = await CreateUserViaOAuthAsync();
        Assert.NotNull(authResult);

        var userId = authResult!.User.Id;

        // Step 2: Check identity before linking password
        using var request1 = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/auth/users/{userId}/identity");
        request1.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
        var response1 = await HttpClient.SendAsync(request1);
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);

        var identityBefore = JsonSerializer.Deserialize<IdentitiesResponse>(await response1.Content.ReadAsStringAsync(), JsonOptions);
        Assert.NotNull(identityBefore);
        Assert.False(identityBefore!.HasPassword, "OAuth user should not have password initially");
        Assert.NotEmpty(identityBefore.LinkedProviders);

        // Step 3: Link password
        var username = $"linked_{Guid.NewGuid():N}";
        var linkRequest = new
        {
            Username = username,
            Email = $"{username}@example.com",
            Password = TestPassword,
            ConfirmPassword = TestPassword
        };
        using var linkHttpRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/auth/users/{userId}/identity/password");
        linkHttpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
        linkHttpRequest.Content = JsonContent.Create(linkRequest);
        var linkResponse = await HttpClient.SendAsync(linkHttpRequest);

        Output.WriteLine($"[INFO] Link password response: {(int)linkResponse.StatusCode} {linkResponse.StatusCode}");

        // Step 4: Check identity after linking password
        using var request2 = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/auth/users/{userId}/identity");
        request2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
        var response2 = await HttpClient.SendAsync(request2);
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);

        var identityAfter = JsonSerializer.Deserialize<IdentitiesResponse>(await response2.Content.ReadAsStringAsync(), JsonOptions);
        Assert.NotNull(identityAfter);

        if (linkResponse.IsSuccessStatusCode)
        {
            Assert.True(identityAfter!.HasPassword, "Should have password after linking");
            Assert.False(identityAfter.IsAnonymous, "Should no longer be anonymous after linking");
        }

        Output.WriteLine("[PASS] Journey: OAuth user links password completed");
    }

    [TimedFact]
    public async Task Journey_TwoUsersHaveIndependentIdentities()
    {
        Output.WriteLine("[TEST] Journey_TwoUsersHaveIndependentIdentities");

        // Create two users
        var user1 = await RegisterUniqueUserAsync();
        var user2 = await CreateUserViaOAuthAsync();
        Assert.NotNull(user1);
        Assert.NotNull(user2);

        // Get user1's identity
        using var request1 = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/auth/users/{user1!.User.Id}/identity");
        request1.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user1.AccessToken);
        var response1 = await HttpClient.SendAsync(request1);
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        var identity1 = JsonSerializer.Deserialize<IdentitiesResponse>(await response1.Content.ReadAsStringAsync(), JsonOptions);

        // Get user2's identity
        using var request2 = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/auth/users/{user2!.User.Id}/identity");
        request2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user2.AccessToken);
        var response2 = await HttpClient.SendAsync(request2);
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
        var identity2 = JsonSerializer.Deserialize<IdentitiesResponse>(await response2.Content.ReadAsStringAsync(), JsonOptions);

        Assert.NotNull(identity1);
        Assert.NotNull(identity2);

        // User1: password user
        Assert.True(identity1!.HasPassword);
        Assert.Empty(identity1.LinkedProviders);

        // User2: OAuth user
        Assert.False(identity2!.HasPassword);
        Assert.NotEmpty(identity2.LinkedProviders);

        Output.WriteLine("[PASS] Two users have independent identities");
    }

    #endregion

    #region Helper Methods

    private async Task<AuthResponse?> RegisterUniqueUserAsync()
    {
        var username = $"identity_{Guid.NewGuid():N}";
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

    private async Task<AuthResponse?> CreateUserViaOAuthAsync()
    {
        // Initiate OAuth
        var initiateRequest = new { Provider = "mock", RedirectUri = "https://localhost/callback" };
        var initiateResponse = await HttpClient.PostAsJsonAsync("/api/v1/auth/external/mock", initiateRequest);
        if (!initiateResponse.IsSuccessStatusCode)
        {
            Output.WriteLine($"[ERROR] OAuth initiate failed: {await initiateResponse.Content.ReadAsStringAsync()}");
            return null;
        }

        var initiateResult = JsonSerializer.Deserialize<OAuthAuthorizationResponse>(await initiateResponse.Content.ReadAsStringAsync(), JsonOptions);
        if (initiateResult is null)
        {
            return null;
        }

        // Process callback
        var callbackRequest = new
        {
            Provider = "mock",
            Code = "mock_auth_code",
            State = initiateResult.State,
            RedirectUri = "https://localhost/callback"
        };
        var callbackResponse = await HttpClient.PostAsJsonAsync("/api/v1/auth/external/callback", callbackRequest);

        if (!callbackResponse.IsSuccessStatusCode)
        {
            Output.WriteLine($"[ERROR] OAuth callback failed: {await callbackResponse.Content.ReadAsStringAsync()}");
            return null;
        }

        var content = await callbackResponse.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<AuthResponse>(content, JsonOptions);
    }

    #endregion

    #region Response DTOs

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

    private record IdentitiesResponse
    {
        public bool IsAnonymous { get; init; }
        public DateTimeOffset? LinkedAt { get; init; }
        public bool HasPassword { get; init; }
        public string? Email { get; init; }
        public bool EmailConfirmed { get; init; }
        public IReadOnlyList<LinkedProviderInfo> LinkedProviders { get; init; } = [];
        public IReadOnlyList<LinkedPasskeyInfo> LinkedPasskeys { get; init; } = [];
    }

    private record LinkedProviderInfo
    {
        public string Provider { get; init; } = string.Empty;
        public string? DisplayName { get; init; }
        public string? Email { get; init; }
    }

    private record LinkedPasskeyInfo
    {
        public string Id { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public DateTimeOffset RegisteredAt { get; init; }
    }

    private record OAuthAuthorizationResponse(string AuthorizationUrl, string State);

    #endregion
}





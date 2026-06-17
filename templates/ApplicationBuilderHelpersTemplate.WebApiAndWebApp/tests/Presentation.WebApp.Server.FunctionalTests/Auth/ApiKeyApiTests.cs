using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Presentation.WebApp.Server.FunctionalTests.Auth;

/// <summary>
/// Functional tests for API Key Management endpoints.
/// Tests create, list, revoke, and usage of API keys.
/// </summary>
public class ApiKeyApiTests : WebAppTestBase
{
    public ApiKeyApiTests(ITestOutputHelper output) : base(output)
    {
    }

    #region List API Keys Tests

    [TimedFact]
    public async Task ListApiKeys_WithoutToken_Returns401()
    {
        Output.WriteLine("[TEST] ListApiKeys_WithoutToken_Returns401");

        var randomUserId = Guid.NewGuid();
        Output.WriteLine($"[STEP] GET /api/v1/auth/users/{randomUserId}/api-keys without token...");
        var response = await HttpClient.GetAsync($"/api/v1/auth/users/{randomUserId}/api-keys");

        Output.WriteLine($"[RECEIVED] Status: {(int)response.StatusCode} {response.StatusCode}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Output.WriteLine("[PASS] Returns 401 without authentication");
    }

    [TimedFact]
    public async Task ListApiKeys_AfterRegister_ReturnsEmptyList()
    {
        Output.WriteLine("[TEST] ListApiKeys_AfterRegister_ReturnsEmptyList");

        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);

        var userId = authResult!.User.Id;
        Output.WriteLine($"[STEP] GET /api/v1/auth/users/{userId}/api-keys with valid token...");
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/auth/users/{userId}/api-keys");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
        var response = await HttpClient.SendAsync(request);

        Output.WriteLine($"[RECEIVED] Status: {(int)response.StatusCode} {response.StatusCode}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Output.WriteLine($"[RECEIVED] Body: {content}");

        var result = JsonSerializer.Deserialize<ApiKeyListResponse>(content, JsonOptions);
        Assert.NotNull(result);
        Assert.Empty(result!.Items);

        Output.WriteLine("[PASS] List API keys returns empty list for new user");
    }

    [TimedFact]
    public async Task ListApiKeys_ForOtherUser_Returns403()
    {
        Output.WriteLine("[TEST] ListApiKeys_ForOtherUser_Returns403");

        // Register two users
        var user1 = await RegisterUniqueUserAsync();
        var user2 = await RegisterUniqueUserAsync();
        Assert.NotNull(user1);
        Assert.NotNull(user2);

        // User1 tries to list User2's API keys
        Output.WriteLine($"[STEP] GET /api/v1/auth/users/{user2!.User.Id}/api-keys with User1's token...");
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/auth/users/{user2.User.Id}/api-keys");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user1!.AccessToken);
        var response = await HttpClient.SendAsync(request);

        Output.WriteLine($"[RECEIVED] Status: {(int)response.StatusCode} {response.StatusCode}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Output.WriteLine("[PASS] Cannot list another user's API keys");
    }

    #endregion

    #region Create API Key Tests

    [TimedFact]
    public async Task CreateApiKey_WithValidData_ReturnsCreatedWithKey()
    {
        Output.WriteLine("[TEST] CreateApiKey_WithValidData_ReturnsCreatedWithKey");

        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);

        var userId = authResult!.User.Id;
        var createRequest = new { Name = "Test API Key" };

        Output.WriteLine($"[STEP] POST /api/v1/auth/users/{userId}/api-keys...");
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/auth/users/{userId}/api-keys");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
        request.Content = JsonContent.Create(createRequest);
        var response = await HttpClient.SendAsync(request);

        Output.WriteLine($"[RECEIVED] Status: {(int)response.StatusCode} {response.StatusCode}");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Output.WriteLine($"[RECEIVED] Body: {content}");

        var result = JsonSerializer.Deserialize<CreateApiKeyResponse>(content, JsonOptions);
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result!.Id);
        Assert.Equal("Test API Key", result.Name);
        Assert.False(string.IsNullOrEmpty(result.Key), "API key JWT should be returned");
        Assert.Null(result.ExpiresAt);

        Output.WriteLine("[PASS] API key created successfully with JWT");
    }

    [TimedFact]
    public async Task CreateApiKey_WithExpiration_ReturnsCreatedWithExpiry()
    {
        Output.WriteLine("[TEST] CreateApiKey_WithExpiration_ReturnsCreatedWithExpiry");

        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);

        var userId = authResult!.User.Id;
        var expiresAt = DateTimeOffset.UtcNow.AddDays(30);
        var createRequest = new { Name = "Expiring Key", ExpiresAt = expiresAt };

        Output.WriteLine($"[STEP] POST /api/v1/auth/users/{userId}/api-keys with expiration...");
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/auth/users/{userId}/api-keys");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
        request.Content = JsonContent.Create(createRequest);
        var response = await HttpClient.SendAsync(request);

        Output.WriteLine($"[RECEIVED] Status: {(int)response.StatusCode} {response.StatusCode}");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<CreateApiKeyResponse>(content, JsonOptions);
        Assert.NotNull(result);
        Assert.NotNull(result!.ExpiresAt);

        Output.WriteLine("[PASS] API key created with expiration date");
    }

    [TimedFact]
    public async Task CreateApiKey_WithPastExpiration_Returns400()
    {
        Output.WriteLine("[TEST] CreateApiKey_WithPastExpiration_Returns400");

        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);

        var userId = authResult!.User.Id;
        var pastDate = DateTimeOffset.UtcNow.AddDays(-1);
        var createRequest = new { Name = "Past Expiry Key", ExpiresAt = pastDate };

        Output.WriteLine($"[STEP] POST /api/v1/auth/users/{userId}/api-keys with past expiration...");
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/auth/users/{userId}/api-keys");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
        request.Content = JsonContent.Create(createRequest);
        var response = await HttpClient.SendAsync(request);

        Output.WriteLine($"[RECEIVED] Status: {(int)response.StatusCode} {response.StatusCode}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Output.WriteLine("[PASS] Rejects API key with past expiration date");
    }

    [TimedFact]
    public async Task CreateApiKey_WithoutToken_Returns401()
    {
        Output.WriteLine("[TEST] CreateApiKey_WithoutToken_Returns401");

        var randomUserId = Guid.NewGuid();
        var createRequest = new { Name = "Unauthorized Key" };

        Output.WriteLine($"[STEP] POST /api/v1/auth/users/{randomUserId}/api-keys without token...");
        var response = await HttpClient.PostAsJsonAsync(
            $"/api/v1/auth/users/{randomUserId}/api-keys", createRequest);

        Output.WriteLine($"[RECEIVED] Status: {(int)response.StatusCode} {response.StatusCode}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Output.WriteLine("[PASS] Returns 401 without authentication");
    }

    [TimedFact]
    public async Task CreateApiKey_ForOtherUser_Returns403()
    {
        Output.WriteLine("[TEST] CreateApiKey_ForOtherUser_Returns403");

        var user1 = await RegisterUniqueUserAsync();
        var user2 = await RegisterUniqueUserAsync();
        Assert.NotNull(user1);
        Assert.NotNull(user2);

        var createRequest = new { Name = "Hacker Key" };

        Output.WriteLine($"[STEP] POST /api/v1/auth/users/{user2!.User.Id}/api-keys with User1's token...");
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/auth/users/{user2.User.Id}/api-keys");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user1!.AccessToken);
        request.Content = JsonContent.Create(createRequest);
        var response = await HttpClient.SendAsync(request);

        Output.WriteLine($"[RECEIVED] Status: {(int)response.StatusCode} {response.StatusCode}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Output.WriteLine("[PASS] Cannot create API key for another user");
    }

    [TimedFact]
    public async Task CreateApiKey_WithEmptyName_Returns400()
    {
        Output.WriteLine("[TEST] CreateApiKey_WithEmptyName_Returns400");

        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);

        var userId = authResult!.User.Id;
        var createRequest = new { Name = "" };

        Output.WriteLine($"[STEP] POST /api/v1/auth/users/{userId}/api-keys with empty name...");
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/auth/users/{userId}/api-keys");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
        request.Content = JsonContent.Create(createRequest);
        var response = await HttpClient.SendAsync(request);

        Output.WriteLine($"[RECEIVED] Status: {(int)response.StatusCode} {response.StatusCode}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Output.WriteLine("[PASS] Rejects API key with empty name");
    }

    #endregion

    #region Revoke API Key Tests

    [TimedFact]
    public async Task RevokeApiKey_ExistingKey_Returns204()
    {
        Output.WriteLine("[TEST] RevokeApiKey_ExistingKey_Returns204");

        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);

        var userId = authResult!.User.Id;

        // Create an API key first
        var createResponse = await CreateApiKeyAsync(userId, authResult.AccessToken, "Key to Revoke");
        Assert.NotNull(createResponse);

        Output.WriteLine($"[STEP] DELETE /api/v1/auth/users/{userId}/api-keys/{createResponse!.Id}...");
        using var request = new HttpRequestMessage(HttpMethod.Delete, 
            $"/api/v1/auth/users/{userId}/api-keys/{createResponse.Id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
        var response = await HttpClient.SendAsync(request);

        Output.WriteLine($"[RECEIVED] Status: {(int)response.StatusCode} {response.StatusCode}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Output.WriteLine("[PASS] API key revoked successfully");
    }

    [TimedFact]
    public async Task RevokeApiKey_NonExistentKey_Returns404()
    {
        Output.WriteLine("[TEST] RevokeApiKey_NonExistentKey_Returns404");

        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);

        var userId = authResult!.User.Id;
        var nonExistentId = Guid.NewGuid();

        Output.WriteLine($"[STEP] DELETE /api/v1/auth/users/{userId}/api-keys/{nonExistentId}...");
        using var request = new HttpRequestMessage(HttpMethod.Delete, 
            $"/api/v1/auth/users/{userId}/api-keys/{nonExistentId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
        var response = await HttpClient.SendAsync(request);

        Output.WriteLine($"[RECEIVED] Status: {(int)response.StatusCode} {response.StatusCode}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Output.WriteLine("[PASS] Returns 404 for non-existent API key");
    }

    [TimedFact]
    public async Task RevokeApiKey_AlreadyRevoked_Returns404()
    {
        Output.WriteLine("[TEST] RevokeApiKey_AlreadyRevoked_Returns404");

        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);

        var userId = authResult!.User.Id;

        // Create and revoke an API key
        var createResponse = await CreateApiKeyAsync(userId, authResult.AccessToken, "Double Revoke Key");
        Assert.NotNull(createResponse);

        // First revoke
        using var request1 = new HttpRequestMessage(HttpMethod.Delete, 
            $"/api/v1/auth/users/{userId}/api-keys/{createResponse!.Id}");
        request1.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
        var response1 = await HttpClient.SendAsync(request1);
        Assert.Equal(HttpStatusCode.NoContent, response1.StatusCode);

        // Second revoke attempt
        Output.WriteLine($"[STEP] DELETE /api/v1/auth/users/{userId}/api-keys/{createResponse.Id} (second time)...");
        using var request2 = new HttpRequestMessage(HttpMethod.Delete, 
            $"/api/v1/auth/users/{userId}/api-keys/{createResponse.Id}");
        request2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
        var response2 = await HttpClient.SendAsync(request2);

        Output.WriteLine($"[RECEIVED] Status: {(int)response2.StatusCode} {response2.StatusCode}");

        Assert.Equal(HttpStatusCode.NotFound, response2.StatusCode);
        Output.WriteLine("[PASS] Returns 404 for already revoked API key");
    }

    [TimedFact]
    public async Task RevokeApiKey_ForOtherUser_Returns403()
    {
        Output.WriteLine("[TEST] RevokeApiKey_ForOtherUser_Returns403");

        var user1 = await RegisterUniqueUserAsync();
        var user2 = await RegisterUniqueUserAsync();
        Assert.NotNull(user1);
        Assert.NotNull(user2);

        // User2 creates an API key
        var createResponse = await CreateApiKeyAsync(user2!.User.Id, user2.AccessToken, "User2 Key");
        Assert.NotNull(createResponse);

        // User1 tries to revoke User2's key
        Output.WriteLine($"[STEP] DELETE /api/v1/auth/users/{user2.User.Id}/api-keys/{createResponse!.Id} with User1's token...");
        using var request = new HttpRequestMessage(HttpMethod.Delete, 
            $"/api/v1/auth/users/{user2.User.Id}/api-keys/{createResponse.Id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user1!.AccessToken);
        var response = await HttpClient.SendAsync(request);

        Output.WriteLine($"[RECEIVED] Status: {(int)response.StatusCode} {response.StatusCode}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Output.WriteLine("[PASS] Cannot revoke another user's API key");
    }

    #endregion

    #region API Key Usage Tests

    [TimedFact]
    public async Task UseApiKey_ForRegularEndpoint_ReturnsSuccess()
    {
        Output.WriteLine("[TEST] UseApiKey_ForRegularEndpoint_ReturnsSuccess");

        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);

        var userId = authResult!.User.Id;

        // Create an API key
        var createResponse = await CreateApiKeyAsync(userId, authResult.AccessToken, "Usage Test Key");
        Assert.NotNull(createResponse);

        // Use the API key to access /auth/me
        Output.WriteLine("[STEP] GET /api/v1/auth/me with API key...");
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", createResponse!.Key);
        var response = await HttpClient.SendAsync(request);

        Output.WriteLine($"[RECEIVED] Status: {(int)response.StatusCode} {response.StatusCode}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var meResult = JsonSerializer.Deserialize<UserInfoResponse>(content, JsonOptions);
        Assert.NotNull(meResult);
        Assert.Equal(userId, meResult!.Id);

        Output.WriteLine("[PASS] API key can access regular endpoints");
    }

    [TimedFact]
    public async Task UseApiKey_AsRefreshToken_Returns401()
    {
        // API keys are standalone JWTs - they cannot be used as refresh tokens.
        // Submitting an API key JWT as the refresh token should fail.
        Output.WriteLine("[TEST] UseApiKey_AsRefreshToken_Returns401");

        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);

        var userId = authResult!.User.Id;

        // Create an API key
        var createResponse = await CreateApiKeyAsync(userId, authResult.AccessToken, "Not A Refresh Token");
        Assert.NotNull(createResponse);

        // Try to use the API key JWT as a refresh token (should fail)
        Output.WriteLine("[STEP] POST /api/v1/auth/refresh with API key as refresh token...");
        var response = await HttpClient.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new { RefreshToken = createResponse!.Key });

        Output.WriteLine($"[RECEIVED] Status: {(int)response.StatusCode} {response.StatusCode}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Output.WriteLine("[PASS] API key cannot be used as a refresh token");
    }

    [TimedFact]
    public async Task UseApiKey_ToCreateAnotherApiKey_Returns403()
    {
        Output.WriteLine("[TEST] UseApiKey_ToCreateAnotherApiKey_Returns403");

        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);

        var userId = authResult!.User.Id;

        // Create an API key
        var createResponse = await CreateApiKeyAsync(userId, authResult.AccessToken, "First Key");
        Assert.NotNull(createResponse);

        // Try to create another API key using the first API key (should be denied)
        Output.WriteLine("[STEP] POST /api/v1/auth/users/{userId}/api-keys with API key...");
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/auth/users/{userId}/api-keys");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", createResponse!.Key);
        request.Content = JsonContent.Create(new { Name = "Second Key via API Key" });
        var response = await HttpClient.SendAsync(request);

        Output.WriteLine($"[RECEIVED] Status: {(int)response.StatusCode} {response.StatusCode}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Output.WriteLine("[PASS] API key cannot create other API keys");
    }

    [TimedFact]
    public async Task UseApiKey_ToListApiKeys_Returns403()
    {
        Output.WriteLine("[TEST] UseApiKey_ToListApiKeys_Returns403");

        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);

        var userId = authResult!.User.Id;

        // Create an API key
        var createResponse = await CreateApiKeyAsync(userId, authResult.AccessToken, "List Deny Key");
        Assert.NotNull(createResponse);

        // Try to list API keys using the API key (should be denied)
        Output.WriteLine("[STEP] GET /api/v1/auth/users/{userId}/api-keys with API key...");
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/auth/users/{userId}/api-keys");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", createResponse!.Key);
        var response = await HttpClient.SendAsync(request);

        Output.WriteLine($"[RECEIVED] Status: {(int)response.StatusCode} {response.StatusCode}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Output.WriteLine("[PASS] API key cannot list API keys");
    }

    [TimedFact]
    public async Task UseApiKey_ToRevokeApiKey_Returns403()
    {
        Output.WriteLine("[TEST] UseApiKey_ToRevokeApiKey_Returns403");

        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);

        var userId = authResult!.User.Id;

        // Create two API keys
        var key1 = await CreateApiKeyAsync(userId, authResult.AccessToken, "Key 1");
        var key2 = await CreateApiKeyAsync(userId, authResult.AccessToken, "Key 2");
        Assert.NotNull(key1);
        Assert.NotNull(key2);

        // Try to revoke key2 using key1 (should be denied)
        Output.WriteLine($"[STEP] DELETE /api/v1/auth/users/{userId}/api-keys/{key2!.Id} with API key...");
        using var request = new HttpRequestMessage(HttpMethod.Delete, 
            $"/api/v1/auth/users/{userId}/api-keys/{key2.Id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key1!.Key);
        var response = await HttpClient.SendAsync(request);

        Output.WriteLine($"[RECEIVED] Status: {(int)response.StatusCode} {response.StatusCode}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Output.WriteLine("[PASS] API key cannot revoke other API keys");
    }

    [TimedFact]
    public async Task UseRevokedApiKey_Returns401()
    {
        Output.WriteLine("[TEST] UseRevokedApiKey_Returns401");

        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);

        var userId = authResult!.User.Id;

        // Create an API key
        var createResponse = await CreateApiKeyAsync(userId, authResult.AccessToken, "Soon to be revoked");
        Assert.NotNull(createResponse);

        // Verify it works first
        using var workingRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        workingRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", createResponse!.Key);
        var workingResponse = await HttpClient.SendAsync(workingRequest);
        Assert.Equal(HttpStatusCode.OK, workingResponse.StatusCode);

        // Revoke the key
        using var revokeRequest = new HttpRequestMessage(HttpMethod.Delete, 
            $"/api/v1/auth/users/{userId}/api-keys/{createResponse.Id}");
        revokeRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
        var revokeResponse = await HttpClient.SendAsync(revokeRequest);
        Assert.Equal(HttpStatusCode.NoContent, revokeResponse.StatusCode);

        // Try to use the revoked key
        Output.WriteLine("[STEP] GET /api/v1/auth/me with revoked API key...");
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", createResponse.Key);
        var response = await HttpClient.SendAsync(request);

        Output.WriteLine($"[RECEIVED] Status: {(int)response.StatusCode} {response.StatusCode}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Output.WriteLine("[PASS] Revoked API key is rejected");
    }

    #endregion

    #region User Journey Tests

    [TimedFact]
    public async Task UserJourney_CreateListRevokeApiKey_FullLifecycle()
    {
        Output.WriteLine("[TEST] UserJourney_CreateListRevokeApiKey_FullLifecycle");

        // Step 1: Register a new user
        Output.WriteLine("[STEP 1] Registering new user...");
        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);
        var userId = authResult!.User.Id;

        // Step 2: List API keys (should be empty)
        Output.WriteLine("[STEP 2] Listing API keys (should be empty)...");
        var listBefore = await ListApiKeysAsync(userId, authResult.AccessToken);
        Assert.NotNull(listBefore);
        Assert.Empty(listBefore!.Items);

        // Step 3: Create first API key
        Output.WriteLine("[STEP 3] Creating first API key...");
        var key1 = await CreateApiKeyAsync(userId, authResult.AccessToken, "Production Bot");
        Assert.NotNull(key1);
        var key1Jwt = key1!.Key;

        // Step 4: Create second API key
        Output.WriteLine("[STEP 4] Creating second API key...");
        var key2 = await CreateApiKeyAsync(userId, authResult.AccessToken, "Development Bot");
        Assert.NotNull(key2);

        // Step 5: List API keys (should have 2)
        Output.WriteLine("[STEP 5] Listing API keys (should have 2)...");
        var listAfterCreate = await ListApiKeysAsync(userId, authResult.AccessToken);
        Assert.NotNull(listAfterCreate);
        Assert.Equal(2, listAfterCreate!.Items.Count);

        // Step 6: Use first API key to access an endpoint
        Output.WriteLine("[STEP 6] Using first API key to access /auth/me...");
        using var meRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        meRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key1Jwt);
        var meResponse = await HttpClient.SendAsync(meRequest);
        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);

        // Step 7: Revoke first API key
        Output.WriteLine("[STEP 7] Revoking first API key...");
        using var revokeRequest = new HttpRequestMessage(HttpMethod.Delete, 
            $"/api/v1/auth/users/{userId}/api-keys/{key1.Id}");
        revokeRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
        var revokeResponse = await HttpClient.SendAsync(revokeRequest);
        Assert.Equal(HttpStatusCode.NoContent, revokeResponse.StatusCode);

        // Step 8: Verify revoked key doesn't work
        Output.WriteLine("[STEP 8] Verifying revoked key is rejected...");
        using var rejectedRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        rejectedRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key1Jwt);
        var rejectedResponse = await HttpClient.SendAsync(rejectedRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, rejectedResponse.StatusCode);

        // Step 9: List API keys (should have 1)
        Output.WriteLine("[STEP 9] Listing API keys (should have 1)...");
        var listAfterRevoke = await ListApiKeysAsync(userId, authResult.AccessToken);
        Assert.NotNull(listAfterRevoke);
        Assert.Single(listAfterRevoke!.Items);
        Assert.Equal("Development Bot", listAfterRevoke.Items[0].Name);

        Output.WriteLine("[PASS] Full API key lifecycle completed successfully");
    }

    [TimedFact]
    public async Task UserJourney_MultipleApiKeysWithDifferentPermissions()
    {
        Output.WriteLine("[TEST] UserJourney_MultipleApiKeysWithDifferentPermissions");

        // Register user
        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);
        var userId = authResult!.User.Id;

        // Create multiple API keys with different purposes
        var tradingKey = await CreateApiKeyAsync(userId, authResult.AccessToken, "Trading Bot");
        var monitorKey = await CreateApiKeyAsync(userId, authResult.AccessToken, "Monitor Script");
        var cicdKey = await CreateApiKeyAsync(userId, authResult.AccessToken, "CI/CD Pipeline", 
            DateTimeOffset.UtcNow.AddHours(1)); // Short-lived key

        Assert.NotNull(tradingKey);
        Assert.NotNull(monitorKey);
        Assert.NotNull(cicdKey);

        // Verify all keys can access /auth/me
        foreach (var key in new[] { tradingKey!, monitorKey!, cicdKey! })
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key.Key);
            var response = await HttpClient.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        // Verify all keys are listed
        var allKeys = await ListApiKeysAsync(userId, authResult.AccessToken);
        Assert.NotNull(allKeys);
        Assert.Equal(3, allKeys!.Items.Count);

        // Verify expiration is set correctly for CI/CD key
        var cicdKeyInfo = allKeys.Items.FirstOrDefault(k => k.Name == "CI/CD Pipeline");
        Assert.NotNull(cicdKeyInfo);
        Assert.NotNull(cicdKeyInfo!.ExpiresAt);

        Output.WriteLine("[PASS] Multiple API keys managed successfully");
    }

    #endregion

    #region Edge Cases and Stress Tests

    [TimedFact]
    public async Task CreateApiKey_MultipleKeys_SucceedsUpToLimit()
    {
        // Note: MaxApiKeysPerUser is 100 in ApiKeyService.cs
        // This test creates a few keys and verifies they can be created,
        // but doesn't test the actual 100 limit due to test runtime concerns.
        Output.WriteLine("[TEST] CreateApiKey_MultipleKeys_SucceedsUpToLimit");

        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);

        var userId = authResult!.User.Id;
        const int testKeyCount = 5;

        // Create several keys to verify the creation works
        for (int i = 1; i <= testKeyCount; i++)
        {
            var response = await CreateApiKeyAsync(userId, authResult.AccessToken, $"Key {i}");
            Assert.NotNull(response);
            Output.WriteLine($"[INFO] Created key {i}/{testKeyCount}");
        }

        // Verify all keys were created
        var allKeys = await ListApiKeysAsync(userId, authResult.AccessToken);
        Assert.NotNull(allKeys);
        Assert.Equal(testKeyCount, allKeys!.Items.Count);

        Output.WriteLine($"[PASS] Successfully created {testKeyCount} API keys (max is 100)");
    }

    [TimedFact]
    public async Task CreateApiKey_WithVeryLongName_Returns400()
    {
        Output.WriteLine("[TEST] CreateApiKey_WithVeryLongName_Returns400");

        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);

        var userId = authResult!.User.Id;
        var longName = new string('A', 101); // Over 100 char limit

        Output.WriteLine($"[STEP] POST /api/v1/auth/users/{userId}/api-keys with 101 char name...");
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/auth/users/{userId}/api-keys");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
        request.Content = JsonContent.Create(new { Name = longName });
        var response = await HttpClient.SendAsync(request);

        Output.WriteLine($"[RECEIVED] Status: {(int)response.StatusCode} {response.StatusCode}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Output.WriteLine("[PASS] Rejects API key with name over 100 characters");
    }

    #endregion

    #region Random User Journey Tests

    [TimedFact]
    public async Task Journey_CreateKeyUseItThenRevoke()
    {
        Output.WriteLine("[TEST] Journey_CreateKeyUseItThenRevoke");

        var auth = await RegisterUniqueUserAsync();
        Assert.NotNull(auth);

        // Create key
        var key = await CreateApiKeyAsync(auth!.User.Id, auth.AccessToken, "Temp Key");
        Assert.NotNull(key);

        // Use key successfully
        using var req1 = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        req1.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key!.Key);
        var res1 = await HttpClient.SendAsync(req1);
        Assert.Equal(HttpStatusCode.OK, res1.StatusCode);

        // Revoke
        using var revokeReq = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/auth/users/{auth.User.Id}/api-keys/{key.Id}");
        revokeReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        var revokeRes = await HttpClient.SendAsync(revokeReq);
        Assert.Equal(HttpStatusCode.NoContent, revokeRes.StatusCode);

        // Use key fails
        using var req2 = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        req2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key.Key);
        var res2 = await HttpClient.SendAsync(req2);
        Assert.Equal(HttpStatusCode.Unauthorized, res2.StatusCode);

        Output.WriteLine("[PASS] Create-use-revoke journey completed");
    }

    [TimedFact]
    public async Task Journey_TwoUsersCannotShareKeys()
    {
        Output.WriteLine("[TEST] Journey_TwoUsersCannotShareKeys");

        var user1 = await RegisterUniqueUserAsync();
        var user2 = await RegisterUniqueUserAsync();
        Assert.NotNull(user1);
        Assert.NotNull(user2);

        // User1 creates key
        var key1 = await CreateApiKeyAsync(user1!.User.Id, user1.AccessToken, "User1 Key");
        Assert.NotNull(key1);

        // User2 tries to revoke User1's key
        using var req = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/auth/users/{user1.User.Id}/api-keys/{key1!.Id}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user2!.AccessToken);
        var res = await HttpClient.SendAsync(req);
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);

        Output.WriteLine("[PASS] Users cannot interfere with each other's keys");
    }

    [TimedFact]
    public async Task Journey_CreateMultipleKeysRevokeOne()
    {
        Output.WriteLine("[TEST] Journey_CreateMultipleKeysRevokeOne");

        var auth = await RegisterUniqueUserAsync();
        Assert.NotNull(auth);

        var key1 = await CreateApiKeyAsync(auth!.User.Id, auth.AccessToken, "Key A");
        var key2 = await CreateApiKeyAsync(auth.User.Id, auth.AccessToken, "Key B");
        var key3 = await CreateApiKeyAsync(auth.User.Id, auth.AccessToken, "Key C");
        Assert.NotNull(key1);
        Assert.NotNull(key2);
        Assert.NotNull(key3);

        // Revoke middle key
        using var revokeReq = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/auth/users/{auth.User.Id}/api-keys/{key2!.Id}");
        revokeReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        await HttpClient.SendAsync(revokeReq);

        // Key1 and Key3 still work
        using var req1 = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        req1.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key1!.Key);
        Assert.Equal(HttpStatusCode.OK, (await HttpClient.SendAsync(req1)).StatusCode);

        using var req3 = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        req3.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key3!.Key);
        Assert.Equal(HttpStatusCode.OK, (await HttpClient.SendAsync(req3)).StatusCode);

        // Key2 is revoked
        using var req2 = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        req2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key2.Key);
        Assert.Equal(HttpStatusCode.Unauthorized, (await HttpClient.SendAsync(req2)).StatusCode);

        // List shows 2 keys
        var list = await ListApiKeysAsync(auth.User.Id, auth.AccessToken);
        Assert.Equal(2, list!.Items.Count);

        Output.WriteLine("[PASS] Revoking one key doesn't affect others");
    }

    [TimedFact]
    public async Task Journey_ApiKeyCannotManageItself()
    {
        Output.WriteLine("[TEST] Journey_ApiKeyCannotManageItself");

        var auth = await RegisterUniqueUserAsync();
        Assert.NotNull(auth);

        var key = await CreateApiKeyAsync(auth!.User.Id, auth.AccessToken, "Self-Aware Key");
        Assert.NotNull(key);

        // Try to revoke itself using the API key
        using var req = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/auth/users/{auth.User.Id}/api-keys/{key!.Id}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key.Key);
        var res = await HttpClient.SendAsync(req);
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);

        Output.WriteLine("[PASS] API key cannot revoke itself");
    }

    [TimedFact]
    public async Task Journey_AccessTokenAndApiKeyBothWork()
    {
        Output.WriteLine("[TEST] Journey_AccessTokenAndApiKeyBothWork");

        var auth = await RegisterUniqueUserAsync();
        Assert.NotNull(auth);

        var key = await CreateApiKeyAsync(auth!.User.Id, auth.AccessToken, "Parallel Key");
        Assert.NotNull(key);

        // Access token works
        using var req1 = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        req1.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        Assert.Equal(HttpStatusCode.OK, (await HttpClient.SendAsync(req1)).StatusCode);

        // API key also works
        using var req2 = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        req2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key!.Key);
        Assert.Equal(HttpStatusCode.OK, (await HttpClient.SendAsync(req2)).StatusCode);

        Output.WriteLine("[PASS] Both token types work simultaneously");
    }

    [TimedFact]
    public async Task Journey_CreateKeyWithSpecialCharactersInName()
    {
        Output.WriteLine("[TEST] Journey_CreateKeyWithSpecialCharactersInName");

        var auth = await RegisterUniqueUserAsync();
        Assert.NotNull(auth);

        var specialName = "My Key! @#$%^&*() - Test_123";
        var key = await CreateApiKeyAsync(auth!.User.Id, auth.AccessToken, specialName);
        Assert.NotNull(key);
        Assert.Equal(specialName, key!.Name);

        var list = await ListApiKeysAsync(auth.User.Id, auth.AccessToken);
        Assert.Contains(list!.Items, k => k.Name == specialName);

        Output.WriteLine("[PASS] Special characters in name preserved");
    }

    [TimedFact]
    public async Task Journey_RevokeAllKeysThenCreateNew()
    {
        Output.WriteLine("[TEST] Journey_RevokeAllKeysThenCreateNew");

        var auth = await RegisterUniqueUserAsync();
        Assert.NotNull(auth);

        // Create and revoke a few keys
        for (int i = 0; i < 3; i++)
        {
            var k = await CreateApiKeyAsync(auth!.User.Id, auth.AccessToken, $"OldKey{i}");
            using var req = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/auth/users/{auth.User.Id}/api-keys/{k!.Id}");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
            await HttpClient.SendAsync(req);
        }

        // List should be empty
        var listBefore = await ListApiKeysAsync(auth!.User.Id, auth.AccessToken);
        Assert.Empty(listBefore!.Items);

        // Create new key
        var newKey = await CreateApiKeyAsync(auth.User.Id, auth.AccessToken, "FreshStart");
        Assert.NotNull(newKey);

        // New key works
        using var useReq = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        useReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newKey!.Key);
        Assert.Equal(HttpStatusCode.OK, (await HttpClient.SendAsync(useReq)).StatusCode);

        Output.WriteLine("[PASS] Can create new keys after revoking all");
    }

    [TimedFact]
    public async Task Journey_ApiKeyReturnsCorrectUserInMe()
    {
        Output.WriteLine("[TEST] Journey_ApiKeyReturnsCorrectUserInMe");

        var auth = await RegisterUniqueUserAsync();
        Assert.NotNull(auth);

        var key = await CreateApiKeyAsync(auth!.User.Id, auth.AccessToken, "Identity Key");
        Assert.NotNull(key);

        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key!.Key);
        var res = await HttpClient.SendAsync(req);
        var content = await res.Content.ReadAsStringAsync();
        var me = JsonSerializer.Deserialize<UserInfoResponse>(content, JsonOptions);

        Assert.Equal(auth.User.Id, me!.Id);
        Assert.Equal(auth.User.Username, me.Username);

        Output.WriteLine("[PASS] API key returns correct user identity");
    }

    [TimedFact]
    public async Task Journey_KeyWithExpirationStillWorksBeforeExpiry()
    {
        Output.WriteLine("[TEST] Journey_KeyWithExpirationStillWorksBeforeExpiry");

        var auth = await RegisterUniqueUserAsync();
        Assert.NotNull(auth);

        var expiresAt = DateTimeOffset.UtcNow.AddHours(24);
        var key = await CreateApiKeyAsync(auth!.User.Id, auth.AccessToken, "24h Key", expiresAt);
        Assert.NotNull(key);
        Assert.NotNull(key!.ExpiresAt);

        // Key works before expiry
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key.Key);
        Assert.Equal(HttpStatusCode.OK, (await HttpClient.SendAsync(req)).StatusCode);

        Output.WriteLine("[PASS] Key with future expiration works");
    }

    [TimedFact]
    public async Task Journey_ListKeysShowsCorrectMetadata()
    {
        Output.WriteLine("[TEST] Journey_ListKeysShowsCorrectMetadata");

        var auth = await RegisterUniqueUserAsync();
        Assert.NotNull(auth);

        var expires = DateTimeOffset.UtcNow.AddDays(7);
        var key = await CreateApiKeyAsync(auth!.User.Id, auth.AccessToken, "Metadata Key", expires);
        Assert.NotNull(key);

        var list = await ListApiKeysAsync(auth.User.Id, auth.AccessToken);
        var listed = list!.Items.First(k => k.Id == key!.Id);

        Assert.Equal("Metadata Key", listed.Name);
        Assert.NotNull(listed.ExpiresAt);
        Assert.True(listed.CreatedAt > DateTimeOffset.UtcNow.AddMinutes(-5));

        Output.WriteLine("[PASS] List returns correct metadata");
    }

    [TimedFact]
    public async Task Journey_RapidCreateAndRevoke()
    {
        Output.WriteLine("[TEST] Journey_RapidCreateAndRevoke");

        var auth = await RegisterUniqueUserAsync();
        Assert.NotNull(auth);

        // Rapidly create and revoke 5 keys
        for (int i = 0; i < 5; i++)
        {
            var k = await CreateApiKeyAsync(auth!.User.Id, auth.AccessToken, $"Rapid{i}");
            Assert.NotNull(k);

            using var req = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/auth/users/{auth.User.Id}/api-keys/{k!.Id}");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
            var res = await HttpClient.SendAsync(req);
            Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
        }

        var list = await ListApiKeysAsync(auth!.User.Id, auth.AccessToken);
        Assert.Empty(list!.Items);

        Output.WriteLine("[PASS] Rapid create/revoke cycles work");
    }

    [TimedFact]
    public async Task Journey_UseKeyThenRefreshAccessToken()
    {
        Output.WriteLine("[TEST] Journey_UseKeyThenRefreshAccessToken");

        var auth = await RegisterUniqueUserAsync();
        Assert.NotNull(auth);

        var key = await CreateApiKeyAsync(auth!.User.Id, auth.AccessToken, "Persistent Key");
        Assert.NotNull(key);

        // Refresh access token
        var refreshRes = await HttpClient.PostAsJsonAsync("/api/v1/auth/refresh", 
            new { RefreshToken = auth.RefreshToken });
        Assert.Equal(HttpStatusCode.OK, refreshRes.StatusCode);

        // API key still works after session refresh
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key!.Key);
        Assert.Equal(HttpStatusCode.OK, (await HttpClient.SendAsync(req)).StatusCode);

        Output.WriteLine("[PASS] API key survives session refresh");
    }

    [TimedFact]
    public async Task Journey_TwoUsersDifferentKeyNames()
    {
        Output.WriteLine("[TEST] Journey_TwoUsersDifferentKeyNames");

        var user1 = await RegisterUniqueUserAsync();
        var user2 = await RegisterUniqueUserAsync();

        // Both users create keys with same name
        var key1 = await CreateApiKeyAsync(user1!.User.Id, user1.AccessToken, "Production");
        var key2 = await CreateApiKeyAsync(user2!.User.Id, user2.AccessToken, "Production");

        Assert.NotNull(key1);
        Assert.NotNull(key2);
        Assert.NotEqual(key1!.Id, key2!.Id);
        Assert.NotEqual(key1.Key, key2.Key);

        // Each key only accesses its owner
        using var req1 = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        req1.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key1.Key);
        var res1 = await HttpClient.SendAsync(req1);
        var me1 = JsonSerializer.Deserialize<UserInfoResponse>(await res1.Content.ReadAsStringAsync(), JsonOptions);
        Assert.Equal(user1.User.Id, me1!.Id);

        using var req2 = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        req2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key2.Key);
        var res2 = await HttpClient.SendAsync(req2);
        var me2 = JsonSerializer.Deserialize<UserInfoResponse>(await res2.Content.ReadAsStringAsync(), JsonOptions);
        Assert.Equal(user2.User.Id, me2!.Id);

        Output.WriteLine("[PASS] Same key name, different users, isolated");
    }

    [TimedFact]
    public async Task Journey_CreateKeyImmediatelyUse()
    {
        Output.WriteLine("[TEST] Journey_CreateKeyImmediatelyUse");

        var auth = await RegisterUniqueUserAsync();
        Assert.NotNull(auth);

        var key = await CreateApiKeyAsync(auth!.User.Id, auth.AccessToken, "Instant Key");
        Assert.NotNull(key);

        // Immediately use without delay
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key!.Key);
        var res = await HttpClient.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        Output.WriteLine("[PASS] Key works immediately after creation");
    }

    [TimedFact]
    public async Task Journey_RevokeNonExistentKeyForUser()
    {
        Output.WriteLine("[TEST] Journey_RevokeNonExistentKeyForUser");

        var auth = await RegisterUniqueUserAsync();
        Assert.NotNull(auth);

        var fakeKeyId = Guid.NewGuid();
        using var req = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/auth/users/{auth!.User.Id}/api-keys/{fakeKeyId}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        var res = await HttpClient.SendAsync(req);

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);

        Output.WriteLine("[PASS] Revoking non-existent key returns 404");
    }

    [TimedFact]
    public async Task Journey_ListAfterMultipleOperations()
    {
        Output.WriteLine("[TEST] Journey_ListAfterMultipleOperations");

        var auth = await RegisterUniqueUserAsync();
        Assert.NotNull(auth);

        // Create 5 keys
        var keys = new List<CreateApiKeyResponse>();
        for (int i = 0; i < 5; i++)
        {
            var k = await CreateApiKeyAsync(auth!.User.Id, auth.AccessToken, $"Key{i}");
            keys.Add(k!);
        }

        // Revoke 2 of them (indices 1 and 3)
        using var rev1 = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/auth/users/{auth!.User.Id}/api-keys/{keys[1].Id}");
        rev1.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        await HttpClient.SendAsync(rev1);

        using var rev2 = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/auth/users/{auth.User.Id}/api-keys/{keys[3].Id}");
        rev2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        await HttpClient.SendAsync(rev2);

        // List should show 3 keys
        var list = await ListApiKeysAsync(auth.User.Id, auth.AccessToken);
        Assert.Equal(3, list!.Items.Count);
        Assert.Contains(list.Items, k => k.Name == "Key0");
        Assert.Contains(list.Items, k => k.Name == "Key2");
        Assert.Contains(list.Items, k => k.Name == "Key4");
        Assert.DoesNotContain(list.Items, k => k.Name == "Key1");
        Assert.DoesNotContain(list.Items, k => k.Name == "Key3");

        Output.WriteLine("[PASS] List correctly reflects create/revoke operations");
    }

    [TimedFact]
    public async Task Journey_ApiKeyCannotAccessOtherUserData()
    {
        Output.WriteLine("[TEST] Journey_ApiKeyCannotAccessOtherUserData");

        var user1 = await RegisterUniqueUserAsync();
        var user2 = await RegisterUniqueUserAsync();

        var key1 = await CreateApiKeyAsync(user1!.User.Id, user1.AccessToken, "User1 Key");
        Assert.NotNull(key1);

        // User1's API key tries to list User2's API keys
        using var req = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/auth/users/{user2!.User.Id}/api-keys");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key1!.Key);
        var res = await HttpClient.SendAsync(req);

        // Should be 403 (API keys can't list any API keys including other users')
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);

        Output.WriteLine("[PASS] API key cannot access other user's data");
    }

    [TimedFact]
    public async Task Journey_CreateKeyWithMaxLengthName()
    {
        Output.WriteLine("[TEST] Journey_CreateKeyWithMaxLengthName");

        var auth = await RegisterUniqueUserAsync();
        Assert.NotNull(auth);

        var maxName = new string('X', 100); // Exactly 100 chars
        var key = await CreateApiKeyAsync(auth!.User.Id, auth.AccessToken, maxName);
        Assert.NotNull(key);
        Assert.Equal(maxName, key!.Name);

        Output.WriteLine("[PASS] 100-char name accepted");
    }

    [TimedFact]
    public async Task Journey_UseMultipleKeysInSequence()
    {
        Output.WriteLine("[TEST] Journey_UseMultipleKeysInSequence");

        var auth = await RegisterUniqueUserAsync();
        Assert.NotNull(auth);

        var keyA = await CreateApiKeyAsync(auth!.User.Id, auth.AccessToken, "KeyA");
        var keyB = await CreateApiKeyAsync(auth.User.Id, auth.AccessToken, "KeyB");
        var keyC = await CreateApiKeyAsync(auth.User.Id, auth.AccessToken, "KeyC");

        // Use each key in sequence
        foreach (var key in new[] { keyA, keyB, keyC })
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key!.Key);
            var res = await HttpClient.SendAsync(req);
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        }

        Output.WriteLine("[PASS] Multiple keys all work in sequence");
    }

    [TimedFact]
    public async Task Journey_EmptyListAfterRegisterThenPopulate()
    {
        Output.WriteLine("[TEST] Journey_EmptyListAfterRegisterThenPopulate");

        var auth = await RegisterUniqueUserAsync();
        Assert.NotNull(auth);

        // Verify empty
        var listBefore = await ListApiKeysAsync(auth!.User.Id, auth.AccessToken);
        Assert.Empty(listBefore!.Items);

        // Add one
        await CreateApiKeyAsync(auth.User.Id, auth.AccessToken, "First");
        var listAfter = await ListApiKeysAsync(auth.User.Id, auth.AccessToken);
        Assert.Single(listAfter!.Items);

        // Add two more
        await CreateApiKeyAsync(auth.User.Id, auth.AccessToken, "Second");
        await CreateApiKeyAsync(auth.User.Id, auth.AccessToken, "Third");
        var listFinal = await ListApiKeysAsync(auth.User.Id, auth.AccessToken);
        Assert.Equal(3, listFinal!.Items.Count);

        Output.WriteLine("[PASS] List grows correctly as keys are added");
    }

    #endregion

    #region Helper Methods

    private async Task<AuthResponse?> RegisterUniqueUserAsync()
    {
        var username = $"apikey_{Guid.NewGuid():N}";
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

    private async Task<CreateApiKeyResponse?> CreateApiKeyAsync(
        Guid userId, 
        string accessToken, 
        string name,
        DateTimeOffset? expiresAt = null)
    {
        var createRequest = expiresAt.HasValue
            ? new { Name = name, ExpiresAt = expiresAt.Value }
            : (object)new { Name = name };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/auth/users/{userId}/api-keys");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = JsonContent.Create(createRequest);
        var response = await HttpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            Output.WriteLine($"[ERROR] Create API key failed: {errorContent}");
            return null;
        }

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<CreateApiKeyResponse>(content, JsonOptions);
    }

    private async Task<ApiKeyListResponse?> ListApiKeysAsync(Guid userId, string accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/auth/users/{userId}/api-keys");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await HttpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            Output.WriteLine($"[ERROR] List API keys failed: {errorContent}");
            return null;
        }

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ApiKeyListResponse>(content, JsonOptions);
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

    private record CreateApiKeyResponse
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = null!;
        public string Key { get; init; } = null!;
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset? ExpiresAt { get; init; }
    }

    private record ApiKeyInfoResponse
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = null!;
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset? ExpiresAt { get; init; }
        public DateTimeOffset? LastUsedAt { get; init; }
    }

    private record ApiKeyListResponse(IReadOnlyList<ApiKeyInfoResponse> Items);

    #endregion
}


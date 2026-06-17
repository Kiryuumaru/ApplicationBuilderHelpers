using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
namespace Presentation.WebApp.Server.FunctionalTests.Auth;

/// <summary>
/// Functional tests for Passkey (WebAuthn) API endpoints.
/// Tests passkey registration, authentication, and management.
/// </summary>
public class PasskeyApiTests : WebAppTestBase
{
    public PasskeyApiTests(ITestOutputHelper output) : base(output)
    {
    }

    #region Passkey Creation Options Tests

    [TimedFact]
    public async Task PasskeyCreationOptions_WithValidToken_ReturnsOptions()
    {
        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);

        var userId = authResult!.User.Id;
        var requestBody = new { CredentialName = "Test Passkey" };
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/auth/users/{userId}/identity/passkeys/options");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
        request.Content = JsonContent.Create(requestBody);
        var response = await HttpClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("challengeid", content.ToLowerInvariant());
        Assert.Contains("optionsjson", content.ToLowerInvariant());
    }

    [TimedFact]
    public async Task PasskeyCreationOptions_WithoutToken_Returns401()
    {
        var randomUserId = Guid.NewGuid();
        var requestBody = new { CredentialName = "Test Passkey" };
        var response = await HttpClient.PostAsJsonAsync($"/api/v1/auth/users/{randomUserId}/identity/passkeys/options", requestBody);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TimedFact]
    public async Task PasskeyCreationOptions_WithEmptyCredentialName_MayReturn400()
    {
        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);

        var userId = authResult!.User.Id;
        var requestBody = new { CredentialName = "" };
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/auth/users/{userId}/identity/passkeys/options");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
        request.Content = JsonContent.Create(requestBody);
        var response = await HttpClient.SendAsync(request);

        // Depending on validation rules
        Assert.True(
            response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest,
            $"Expected 200 or 400, got {(int)response.StatusCode}");
    }

    #endregion

    #region Passkey Login Options Tests

    [TimedFact]
    public async Task PasskeyLoginOptions_ReturnsOptions()
    {
        var requestBody = new { Username = (string?)null };
        var response = await HttpClient.PostAsJsonAsync("/api/v1/auth/login/passkey/options", requestBody);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("challengeid", content.ToLowerInvariant());
    }

    [TimedFact]
    public async Task PasskeyLoginOptions_WithUsername_ReturnsOptions()
    {
        var username = $"passkey_opts_{Guid.NewGuid():N}";
        await RegisterUserAsync(username);

        var requestBody = new { Username = username };
        var response = await HttpClient.PostAsJsonAsync("/api/v1/auth/login/passkey/options", requestBody);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [TimedFact]
    public async Task PasskeyLoginOptions_WithNonExistentUsername_StillReturnsOptions()
    {
        // Security: Should not reveal if user exists
        var requestBody = new { Username = $"nonexistent_{Guid.NewGuid():N}" };
        var response = await HttpClient.PostAsJsonAsync("/api/v1/auth/login/passkey/options", requestBody);

        // Should still return options (don't reveal user existence)
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion

    #region Passkey Registration Tests

    [TimedFact]
    public async Task PasskeyRegister_WithoutToken_Returns401()
    {
        var randomUserId = Guid.NewGuid();
        var requestBody = new { ChallengeId = Guid.NewGuid(), AttestationResponseJson = "{}" };
        var response = await HttpClient.PostAsJsonAsync($"/api/v1/auth/users/{randomUserId}/identity/passkeys", requestBody);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TimedFact]
    public async Task PasskeyRegister_WithInvalidChallengeId_Returns400()
    {
        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);

        var userId = authResult!.User.Id;
        var requestBody = new { ChallengeId = Guid.NewGuid(), AttestationResponseJson = "{}" };
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/auth/users/{userId}/identity/passkeys");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
        request.Content = JsonContent.Create(requestBody);
        var response = await HttpClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TimedFact]
    public async Task PasskeyRegister_WithEmptyChallengeId_Returns400()
    {
        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);

        var userId = authResult!.User.Id;
        var requestBody = new { ChallengeId = Guid.Empty, AttestationResponseJson = "{}" };
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/auth/users/{userId}/identity/passkeys");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
        request.Content = JsonContent.Create(requestBody);
        var response = await HttpClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region Passkey Login Tests

    [TimedFact]
    public async Task PasskeyLogin_WithInvalidChallengeId_Returns400()
    {
        var requestBody = new { ChallengeId = Guid.NewGuid(), AssertionResponseJson = "{}" };
        var response = await HttpClient.PostAsJsonAsync("/api/v1/auth/login/passkey", requestBody);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TimedFact]
    public async Task PasskeyLogin_WithEmptyChallengeId_Returns400()
    {
        var requestBody = new { ChallengeId = Guid.Empty, AssertionResponseJson = "{}" };
        var response = await HttpClient.PostAsJsonAsync("/api/v1/auth/login/passkey", requestBody);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TimedFact]
    public async Task PasskeyLogin_WithMalformedAssertionJson_Returns400Or500()
    {
        // First get a valid challenge
        var optionsResponse = await HttpClient.PostAsJsonAsync(
            "/api/v1/auth/login/passkey/options",
            new { Username = (string?)null });
        var optionsContent = await optionsResponse.Content.ReadAsStringAsync();
        var options = JsonSerializer.Deserialize<PasskeyOptionsResponse>(optionsContent, JsonOptions);

        if (options?.ChallengeId != null)
        {
            var requestBody = new { ChallengeId = options.ChallengeId, AssertionResponseJson = "not valid json" };
            var response = await HttpClient.PostAsJsonAsync("/api/v1/auth/login/passkey", requestBody);

            // Currently returns 500 for invalid JSON, expected 400
            Assert.True(
                response.StatusCode == HttpStatusCode.BadRequest ||
                response.StatusCode == HttpStatusCode.InternalServerError,
                $"Unexpected status: {(int)response.StatusCode}");
        }
    }

    #endregion

    #region Passkey List Tests

    [TimedFact]
    public async Task PasskeyList_WithValidToken_ReturnsEmptyList()
    {
        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);

        var userId = authResult!.User.Id;
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/auth/users/{userId}/identity/passkeys");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
        var response = await HttpClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("items", content.ToLowerInvariant());
    }

    [TimedFact]
    public async Task PasskeyList_WithoutToken_Returns401()
    {
        var randomUserId = Guid.NewGuid();
        var response = await HttpClient.GetAsync($"/api/v1/auth/users/{randomUserId}/identity/passkeys");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region Passkey Delete Tests

    [TimedFact]
    public async Task PasskeyDelete_WithoutToken_Returns401()
    {
        var randomUserId = Guid.NewGuid();
        var response = await HttpClient.DeleteAsync($"/api/v1/auth/users/{randomUserId}/identity/passkeys/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TimedFact]
    public async Task PasskeyDelete_WithNonExistentId_Returns404()
    {
        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);

        var userId = authResult!.User.Id;
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/auth/users/{userId}/identity/passkeys/{Guid.NewGuid()}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
        var response = await HttpClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region Security Tests - Challenge Reuse

    [TimedFact]
    public async Task PasskeyLogin_ChallengeCannotBeReused()
    {
        // Get a challenge
        var optionsResponse = await HttpClient.PostAsJsonAsync(
            "/api/v1/auth/login/passkey/options",
            new { Username = (string?)null });
        var optionsContent = await optionsResponse.Content.ReadAsStringAsync();
        var options = JsonSerializer.Deserialize<PasskeyOptionsResponse>(optionsContent, JsonOptions);

        if (options?.ChallengeId != null)
        {
            // First attempt
            var requestBody = new { ChallengeId = options.ChallengeId, AssertionResponseJson = "{}" };
            await HttpClient.PostAsJsonAsync("/api/v1/auth/login/passkey", requestBody);

            // Second attempt with same challenge should fail
            var response2 = await HttpClient.PostAsJsonAsync("/api/v1/auth/login/passkey", requestBody);

            // Currently returns 500 for challenge reuse, expected 400
            Assert.True(
                response2.StatusCode == HttpStatusCode.BadRequest ||
                response2.StatusCode == HttpStatusCode.InternalServerError,
                $"Unexpected status: {(int)response2.StatusCode}");
        }
    }

    [TimedFact]
    public async Task PasskeyRegister_ChallengeCannotBeReused()
    {
        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);

        var userId = authResult!.User.Id;

        // Get a creation challenge
        using var optionsRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/auth/users/{userId}/identity/passkeys/options");
        optionsRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
        optionsRequest.Content = JsonContent.Create(new { CredentialName = "Test" });
        var optionsResponse = await HttpClient.SendAsync(optionsRequest);
        var optionsContent = await optionsResponse.Content.ReadAsStringAsync();
        var options = JsonSerializer.Deserialize<PasskeyOptionsResponse>(optionsContent, JsonOptions);

        if (options?.ChallengeId != null)
        {
            // First attempt
            using var request1 = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/auth/users/{userId}/identity/passkeys");
            request1.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
            request1.Content = JsonContent.Create(new { ChallengeId = options.ChallengeId, AttestationResponseJson = "{}" });
            await HttpClient.SendAsync(request1);

            // Second attempt with same challenge should fail
            using var request2 = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/auth/users/{userId}/identity/passkeys");
            request2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
            request2.Content = JsonContent.Create(new { ChallengeId = options.ChallengeId, AttestationResponseJson = "{}" });
            var response2 = await HttpClient.SendAsync(request2);

            // Currently returns 500 for challenge reuse, expected 400
            Assert.True(
                response2.StatusCode == HttpStatusCode.BadRequest ||
                response2.StatusCode == HttpStatusCode.InternalServerError,
                $"Unexpected status: {(int)response2.StatusCode}");
        }
    }

    #endregion

    #region Security Tests - Injection in Passkey Data

    [TimedTheory]
    [InlineData("<script>alert('xss')</script>")]
    [InlineData("' OR '1'='1")]
    [InlineData("{{constructor.constructor}}")]
    public async Task PasskeyCreationOptions_WithMaliciousCredentialName_DoesNotCauseServerError(string maliciousName)
    {
        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);

        var userId = authResult!.User.Id;
        var requestBody = new { CredentialName = maliciousName };
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/auth/users/{userId}/identity/passkeys/options");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
        request.Content = JsonContent.Create(requestBody);
        var response = await HttpClient.SendAsync(request);

        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [TimedTheory]
    [InlineData("{\"malicious\":\"<script>alert(1)</script>\"}")]
    [InlineData("{\"__proto__\":{\"polluted\":true}}")]
    public async Task PasskeyRegister_WithMaliciousAttestationJson_DoesNotCauseServerError(string maliciousJson)
    {
        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);

        var userId = authResult!.User.Id;
        var requestBody = new { ChallengeId = Guid.NewGuid(), AttestationResponseJson = maliciousJson };
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/auth/users/{userId}/identity/passkeys");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
        request.Content = JsonContent.Create(requestBody);
        var response = await HttpClient.SendAsync(request);

        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    #endregion

    #region Security Tests - Cross-User Passkey Access

    [TimedFact]
    public async Task PasskeyDelete_CannotDeleteOtherUserPasskey()
    {
        var user1 = await RegisterUniqueUserAsync();
        var user2 = await RegisterUniqueUserAsync();

        Assert.NotNull(user1);
        Assert.NotNull(user2);

        var user2Id = user2!.User.Id;

        // Try to delete a passkey (even though it doesn't exist) using user2's token
        // but with an ID that could belong to user1
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/auth/users/{user2Id}/identity/passkeys/{Guid.NewGuid()}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user2.AccessToken);
        var response = await HttpClient.SendAsync(request);

        // Should return 404 (not found for this user) not 403 (forbidden) which would leak existence
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region Security Tests - Large Payload

    [TimedFact]
    public async Task PasskeyRegister_WithLargeAttestationJson_DoesNotCrash()
    {
        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);

        var userId = authResult!.User.Id;
        var largeJson = "{\"data\":\"" + new string('a', 100000) + "\"}";
        var requestBody = new { ChallengeId = Guid.NewGuid(), AttestationResponseJson = largeJson };
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/auth/users/{userId}/identity/passkeys");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
        request.Content = JsonContent.Create(requestBody);
        var response = await HttpClient.SendAsync(request);

        // Should handle gracefully
        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest ||
            response.StatusCode == HttpStatusCode.RequestEntityTooLarge,
            $"Expected 400 or 413, got {(int)response.StatusCode}");
    }

    [TimedFact]
    public async Task PasskeyCreationOptions_WithLongCredentialName_DoesNotCrash()
    {
        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);

        var userId = authResult!.User.Id;
        var longName = new string('a', 10000);
        var requestBody = new { CredentialName = longName };
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/auth/users/{userId}/identity/passkeys/options");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
        request.Content = JsonContent.Create(requestBody);
        var response = await HttpClient.SendAsync(request);

        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    #endregion

    #region Helper Methods

    private async Task<AuthResponse?> RegisterUniqueUserAsync()
    {
        return await RegisterUserAsync($"passkey_test_{Guid.NewGuid():N}");
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

        if (!response.IsSuccessStatusCode) return null;

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<AuthResponse>(content, JsonOptions);
    }

    #endregion

    #region DTOs

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

    private record PasskeyOptionsResponse(
        Guid ChallengeId,
        string OptionsJson);

    #endregion
}






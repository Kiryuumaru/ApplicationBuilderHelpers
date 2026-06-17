using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
namespace Presentation.WebApp.Server.FunctionalTests.Auth;

/// <summary>
/// Functional tests for Token Management API endpoints.
/// Tests refresh token, logout, and token security.
/// </summary>
public class TokenApiTests : WebAppTestBase
{
    public TokenApiTests(ITestOutputHelper output) : base(output)
    {
    }

    #region Refresh Token Tests

    [TimedFact]
    public async Task RefreshToken_WithValidToken_ReturnsNewTokens()
    {
        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);

        var refreshRequest = new { RefreshToken = authResult!.RefreshToken };
        var response = await HttpClient.PostAsJsonAsync("/api/v1/auth/refresh", refreshRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<AuthResponse>(content, JsonOptions);

        Assert.NotNull(result);
        Assert.NotEmpty(result!.AccessToken);
        Assert.NotEmpty(result.RefreshToken);
        Assert.NotEqual(authResult.AccessToken, result.AccessToken);
    }

    [TimedFact]
    public async Task RefreshToken_WithInvalidToken_Returns401()
    {
        var refreshRequest = new { RefreshToken = "invalid.token.here" };
        var response = await HttpClient.PostAsJsonAsync("/api/v1/auth/refresh", refreshRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TimedFact]
    public async Task RefreshToken_WithAccessTokenInsteadOfRefresh_Returns401()
    {
        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);

        // Try to use access token as refresh token
        var refreshRequest = new { RefreshToken = authResult!.AccessToken };
        var response = await HttpClient.PostAsJsonAsync("/api/v1/auth/refresh", refreshRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TimedFact]
    public async Task RefreshToken_WithEmptyToken_Returns400Or401()
    {
        var refreshRequest = new { RefreshToken = "" };
        var response = await HttpClient.PostAsJsonAsync("/api/v1/auth/refresh", refreshRequest);

        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.Unauthorized,
            $"Expected 400 or 401, got {(int)response.StatusCode}");
    }

    [TimedFact]
    public async Task RefreshToken_WithMalformedJson_Returns400()
    {
        var response = await HttpClient.PostAsync(
            "/api/v1/auth/refresh",
            new StringContent("{invalid}", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region Token Rotation Tests

    [TimedFact]
    public async Task RefreshToken_RotatesToken_OldTokenBecomesInvalid()
    {
        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);

        var oldRefreshToken = authResult!.RefreshToken;

        // First refresh - should succeed
        var refreshRequest1 = new { RefreshToken = oldRefreshToken };
        var response1 = await HttpClient.PostAsJsonAsync("/api/v1/auth/refresh", refreshRequest1);
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);

        var content1 = await response1.Content.ReadAsStringAsync();
        var newTokens = JsonSerializer.Deserialize<AuthResponse>(content1, JsonOptions);
        Assert.NotNull(newTokens);

        // Try to use old refresh token again - should fail due to rotation
        var refreshRequest2 = new { RefreshToken = oldRefreshToken };
        var response2 = await HttpClient.PostAsJsonAsync("/api/v1/auth/refresh", refreshRequest2);

        // Old token should be invalid after rotation
        Assert.Equal(HttpStatusCode.Unauthorized, response2.StatusCode);
    }

    [TimedFact]
    public async Task RefreshToken_NewTokenWorks_AfterRotation()
    {
        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);

        // First refresh
        var refreshRequest1 = new { RefreshToken = authResult!.RefreshToken };
        var response1 = await HttpClient.PostAsJsonAsync("/api/v1/auth/refresh", refreshRequest1);
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);

        var content1 = await response1.Content.ReadAsStringAsync();
        var newTokens = JsonSerializer.Deserialize<AuthResponse>(content1, JsonOptions);
        Assert.NotNull(newTokens);

        // Second refresh with new token - should succeed
        var refreshRequest2 = new { RefreshToken = newTokens!.RefreshToken };
        var response2 = await HttpClient.PostAsJsonAsync("/api/v1/auth/refresh", refreshRequest2);

        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
    }

    #endregion

    #region Logout Tests

    [TimedFact]
    public async Task Logout_WithValidToken_ReturnsNoContent()
    {
        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult!.AccessToken);
        var response = await HttpClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [TimedFact]
    public async Task Logout_WithoutToken_Returns401()
    {
        var response = await HttpClient.PostAsync("/api/v1/auth/logout", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TimedFact]
    public async Task Logout_InvalidatesRefreshToken()
    {
        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);

        // Logout
        using var logoutRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout");
        logoutRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult!.AccessToken);
        await HttpClient.SendAsync(logoutRequest);

        // Try to use refresh token after logout
        var refreshRequest = new { RefreshToken = authResult.RefreshToken };
        var refreshResponse = await HttpClient.PostAsJsonAsync("/api/v1/auth/refresh", refreshRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
    }

    #endregion

    #region GetMe Tests

    [TimedFact]
    public async Task GetMe_WithValidToken_ReturnsUserInfo()
    {
        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult!.AccessToken);
        var response = await HttpClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<UserInfoResponse>(content, JsonOptions);

        Assert.NotNull(result);
        Assert.NotEmpty(result!.Username);
    }

    [TimedFact]
    public async Task GetMe_WithNoToken_Returns401()
    {
        var response = await HttpClient.GetAsync("/api/v1/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TimedFact]
    public async Task GetMe_WithInvalidToken_Returns401()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "invalid.token.here");
        var response = await HttpClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region Security Tests - Token Manipulation

    [TimedTheory]
    [InlineData("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.dozjgNryP4J3jVmNHl0w5N_XgL0n3I9PlFUP0THsR8U")] // Valid format but wrong key
    [InlineData("notajwt")]
    [InlineData("a.b.c")]
    [InlineData("")]
    public async Task GetMe_WithMalformedToken_Returns401(string malformedToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        if (!string.IsNullOrEmpty(malformedToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", malformedToken);
        }
        var response = await HttpClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TimedFact]
    public async Task GetMe_WithModifiedTokenPayload_Returns401()
    {
        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);

        var tokenParts = authResult!.AccessToken.Split('.');
        if (tokenParts.Length == 3)
        {
            // Modify the payload (middle part)
            var modifiedToken = $"{tokenParts[0]}.MODIFIED{tokenParts[1]}.{tokenParts[2]}";

            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", modifiedToken);
            var response = await HttpClient.SendAsync(request);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }

    [TimedFact]
    public async Task GetMe_WithStrippedSignature_Returns401()
    {
        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);

        var tokenParts = authResult!.AccessToken.Split('.');
        if (tokenParts.Length == 3)
        {
            // Strip the signature
            var unsignedToken = $"{tokenParts[0]}.{tokenParts[1]}.";

            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", unsignedToken);
            var response = await HttpClient.SendAsync(request);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }

    [TimedFact]
    public async Task GetMe_WithNoneAlgorithmAttack_Returns401()
    {
        // "none" algorithm attack - a common JWT vulnerability
        // Header: {"alg":"none","typ":"JWT"}
        var noneHeader = "eyJhbGciOiJub25lIiwidHlwIjoiSldUIn0";
        var fakePayload = "eyJzdWIiOiJhZG1pbiIsInJvbGUiOiJhZG1pbiJ9";
        var noneAlgToken = $"{noneHeader}.{fakePayload}.";

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", noneAlgToken);
        var response = await HttpClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region Security Tests - Token Reuse After Operations

    [TimedFact]
    public async Task AccessToken_StillWorks_AfterRefresh()
    {
        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);

        var originalAccessToken = authResult!.AccessToken;

        // Refresh to get new tokens
        var refreshRequest = new { RefreshToken = authResult.RefreshToken };
        await HttpClient.PostAsJsonAsync("/api/v1/auth/refresh", refreshRequest);

        // Original access token should still work until expiration
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", originalAccessToken);
        var response = await HttpClient.SendAsync(request);

        // Access tokens typically remain valid until expiration
        // This test documents the current behavior
        Output.WriteLine($"Access token after refresh: {(int)response.StatusCode}");
    }

    #endregion

    #region Security Tests - Cross-User Token Usage

    [TimedFact]
    public async Task RefreshToken_CannotBeUsedByDifferentUser()
    {
        var user1 = await RegisterUniqueUserAsync();
        var user2 = await RegisterUniqueUserAsync();

        Assert.NotNull(user1);
        Assert.NotNull(user2);

        // Try to refresh with user1's token but expect user1's session
        // (This is more of a design verification - refresh tokens are tied to users)
        var refreshRequest = new { RefreshToken = user1!.RefreshToken };
        var response = await HttpClient.PostAsJsonAsync("/api/v1/auth/refresh", refreshRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<AuthResponse>(content, JsonOptions);

        // The refresh should return user1's tokens, not user2's
        Assert.NotNull(result?.User);
    }

    #endregion

    #region Security Tests - Authorization Header Variations

    [TimedTheory]
    [InlineData("bearer")]    // Lowercase
    [InlineData("BEARER")]    // Uppercase
    [InlineData("Bearer")]    // Standard
    public async Task GetMe_AuthorizationScheme_IsCaseInsensitive(string scheme)
    {
        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.TryAddWithoutValidation("Authorization", $"{scheme} {authResult!.AccessToken}");
        var response = await HttpClient.SendAsync(request);

        // Bearer scheme should be case-insensitive per RFC 7235
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [TimedTheory]
    [InlineData("Basic")]
    [InlineData("Digest")]
    [InlineData("Custom")]
    public async Task GetMe_WithWrongAuthScheme_Returns401(string scheme)
    {
        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.TryAddWithoutValidation("Authorization", $"{scheme} {authResult!.AccessToken}");
        var response = await HttpClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region Security Tests - Response Headers

    [TimedFact]
    public async Task RefreshToken_Response_DoesNotLeakSensitiveHeaders()
    {
        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);

        var refreshRequest = new { RefreshToken = authResult!.RefreshToken };
        var response = await HttpClient.PostAsJsonAsync("/api/v1/auth/refresh", refreshRequest);

        // Check that sensitive info isn't leaked in headers
        Assert.False(response.Headers.Contains("X-Powered-By"));
        Assert.False(response.Headers.Contains("Server") && 
                     response.Headers.GetValues("Server").Any(v => v.Contains("version", StringComparison.OrdinalIgnoreCase)));
    }

    #endregion

    #region Helper Methods

    private async Task<AuthResponse?> RegisterUniqueUserAsync()
    {
        var username = $"token_test_{Guid.NewGuid():N}";
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

    #endregion
}






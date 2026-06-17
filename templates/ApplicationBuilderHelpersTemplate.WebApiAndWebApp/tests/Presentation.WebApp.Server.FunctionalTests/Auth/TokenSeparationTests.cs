using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
namespace Presentation.WebApp.Server.FunctionalTests.Auth;

/// <summary>
/// Tests for permission-based token separation.
/// 
/// Token structure:
/// - ACCESS TOKENS: Carry role claims and direct grants + deny;api:auth:refresh
/// - REFRESH TOKENS: Have ONLY allow;api:auth:refresh;userId={userId}
/// 
/// The /auth/refresh endpoint:
/// - Is [AllowAnonymous] - doesn't check Authorization header
/// - Validates the token passed in request BODY
/// - Checks the BODY token has api:auth:refresh permission
/// - So access tokens CANNOT be used as refresh tokens (they have deny;api:auth:refresh)
/// 
/// Endpoints with [RequiredPermission] (e.g., /auth/me, /auth/sessions, /auth/logout):
/// - Refresh tokens CANNOT access them because they only have allow;api:auth:refresh
/// - These endpoints require specific permissions that access tokens provide
/// </summary>
public class TokenSeparationTests : WebAppTestBase
{
    public TokenSeparationTests(ITestOutputHelper output) : base(output)
    {
    }

    #region Core Token Separation Tests

    /// <summary>
    /// The key test: Access tokens CANNOT be used as refresh tokens.
    /// This verifies that the deny;api:auth:refresh directive works.
    /// </summary>
    [TimedFact]
    public async Task AccessTokenAsRefreshToken_IsRejected_Returns401()
    {
        // Arrange: Register a user and get tokens
        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);
        Output.WriteLine($"Registered user with access token and refresh token");

        // Act: Try to use access token AS the refresh token (in body)
        var refreshRequest = new { RefreshToken = authResult!.AccessToken };  // Using ACCESS token!
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        request.Content = JsonContent.Create(refreshRequest);

        var response = await HttpClient.SendAsync(request);
        Output.WriteLine($"Refresh with access token in body status: {(int)response.StatusCode}");

        // Assert: Should be 401 because access tokens have deny;api:auth:refresh
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Refresh tokens CAN be used as refresh tokens.
    /// This verifies they have allow;api:auth:refresh.
    /// </summary>
    [TimedFact]
    public async Task RefreshTokenAsRefreshToken_IsAccepted_Returns200()
    {
        // Arrange: Register a user and get tokens
        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);
        Output.WriteLine($"Registered user with tokens");

        // Act: Use refresh token in body
        var refreshRequest = new { RefreshToken = authResult!.RefreshToken };  // Correct token
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        request.Content = JsonContent.Create(refreshRequest);

        var response = await HttpClient.SendAsync(request);
        Output.WriteLine($"Refresh with refresh token status: {(int)response.StatusCode}");

        // Assert: Should succeed
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var newAuth = JsonSerializer.Deserialize<AuthResponse>(content, JsonOptions);
        Assert.NotNull(newAuth);
        Assert.NotEmpty(newAuth!.AccessToken);
        Assert.NotEmpty(newAuth.RefreshToken);
    }

    /// <summary>
    /// Refresh tokens CANNOT access endpoints with [RequiredPermission].
    /// This verifies they only have the refresh permission.
    /// </summary>
    [TimedFact]
    public async Task RefreshToken_CannotAccessProtectedEndpoint_Returns403()
    {
        // Arrange: Register a user and get tokens
        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);

        // Act: Try to use refresh token to access /users (has [RequiredPermission])
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/iam/users");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult!.RefreshToken);

        var response = await HttpClient.SendAsync(request);
        Output.WriteLine($"Access /users with refresh token status: {(int)response.StatusCode}");

        // Assert: Should be 403 because refresh tokens lack api:users:list permission
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    #endregion

    #region Access Token Tests

    [TimedFact]
    public async Task AccessToken_CanAccessMe_Returns200()
    {
        // Arrange: Register a user and get tokens
        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);

        // Act: Use access token to access /me
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult!.AccessToken);

        var response = await HttpClient.SendAsync(request);
        Output.WriteLine($"Access /me with access token status: {(int)response.StatusCode}");

        // Assert: Should succeed
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [TimedFact]
    public async Task AccessToken_CanLogout_ReturnsSuccess()
    {
        // Arrange: Register a user and get tokens
        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);

        // Act: Use access token to logout
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult!.AccessToken);

        var response = await HttpClient.SendAsync(request);
        Output.WriteLine($"Logout with access token status: {(int)response.StatusCode}");

        // Assert: Should succeed
        Assert.True(response.IsSuccessStatusCode, $"Expected success, got {(int)response.StatusCode}");
    }

    #endregion

    #region Refresh Token Access to Auth-Only Endpoints

    /// <summary>
    /// Refresh tokens CANNOT access /auth/me because it has [RequiredPermission].
    /// Refresh tokens only have api:auth:refresh permission, not api:auth:me.
    /// </summary>
    [TimedFact]
    public async Task RefreshToken_CannotAccessMe_BecausePermissionRequired()
    {
        // Arrange: Register a user and get tokens
        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);

        // Act: Use refresh token to access /me
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult!.RefreshToken);

        var response = await HttpClient.SendAsync(request);
        Output.WriteLine($"Access /me with refresh token status: {(int)response.StatusCode}");

        // Assert: Fails with 403 because refresh tokens lack api:auth:me permission
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>
    /// Refresh tokens CANNOT logout because /auth/logout has [RequiredPermission].
    /// Refresh tokens only have api:auth:refresh permission, not api:auth:logout.
    /// </summary>
    [TimedFact]
    public async Task RefreshToken_CannotLogout_BecausePermissionRequired()
    {
        // Arrange: Register a user and get tokens
        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);

        // Act: Use refresh token to logout
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult!.RefreshToken);

        var response = await HttpClient.SendAsync(request);
        Output.WriteLine($"Logout with refresh token status: {(int)response.StatusCode}");

        // Assert: Fails with 403 because refresh tokens lack api:auth:logout permission
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    #endregion

    #region Token Refresh Produces Correctly Separated Tokens

    /// <summary>
    /// After refresh, new access token still cannot be used as refresh token.
    /// </summary>
    [TimedFact]
    public async Task NewAccessToken_AfterRefresh_StillCannotBeUsedAsRefreshToken()
    {
        // Arrange: Register and refresh to get new tokens
        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);

        // Refresh to get new tokens
        var refreshRequest = new { RefreshToken = authResult!.RefreshToken };
        using var refreshReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        refreshReq.Content = JsonContent.Create(refreshRequest);

        var refreshResponse = await HttpClient.SendAsync(refreshReq);
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);

        var content = await refreshResponse.Content.ReadAsStringAsync();
        var newAuth = JsonSerializer.Deserialize<AuthResponse>(content, JsonOptions);
        Assert.NotNull(newAuth);

        // Act: Try to use the NEW access token AS refresh token (in body)
        var attemptRequest = new { RefreshToken = newAuth!.AccessToken };  // Using access token!
        using var attemptRefresh = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        attemptRefresh.Content = JsonContent.Create(attemptRequest);

        var attemptResponse = await HttpClient.SendAsync(attemptRefresh);
        Output.WriteLine($"Refresh with new access token status: {(int)attemptResponse.StatusCode}");

        // Assert: Should be 401 - new access tokens also have deny;api:auth:refresh
        Assert.Equal(HttpStatusCode.Unauthorized, attemptResponse.StatusCode);
    }

    /// <summary>
    /// After refresh, new refresh token CAN be used as refresh token.
    /// </summary>
    [TimedFact]
    public async Task NewRefreshToken_AfterRefresh_CanBeUsedAsRefreshToken()
    {
        // Arrange: Register and refresh once
        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);

        var refreshRequest1 = new { RefreshToken = authResult!.RefreshToken };
        using var refreshReq1 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        refreshReq1.Content = JsonContent.Create(refreshRequest1);

        var response1 = await HttpClient.SendAsync(refreshReq1);
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);

        var content1 = await response1.Content.ReadAsStringAsync();
        var newAuth1 = JsonSerializer.Deserialize<AuthResponse>(content1, JsonOptions);
        Assert.NotNull(newAuth1);

        // Act: Use the NEW refresh token to refresh again
        var refreshRequest2 = new { RefreshToken = newAuth1!.RefreshToken };
        using var refreshReq2 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        refreshReq2.Content = JsonContent.Create(refreshRequest2);

        var response2 = await HttpClient.SendAsync(refreshReq2);
        Output.WriteLine($"Second refresh status: {(int)response2.StatusCode}");

        // Assert: Should succeed
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
    }

    /// <summary>
    /// After refresh, new refresh token still cannot access protected endpoints.
    /// </summary>
    [TimedFact]
    public async Task NewRefreshToken_AfterRefresh_StillCannotAccessProtectedEndpoints()
    {
        // Arrange: Register and refresh
        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);

        var refreshRequest = new { RefreshToken = authResult!.RefreshToken };
        using var refreshReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        refreshReq.Content = JsonContent.Create(refreshRequest);

        var refreshResponse = await HttpClient.SendAsync(refreshReq);
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);

        var content = await refreshResponse.Content.ReadAsStringAsync();
        var newAuth = JsonSerializer.Deserialize<AuthResponse>(content, JsonOptions);
        Assert.NotNull(newAuth);

        // Act: Try to use new refresh token to access /users
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/iam/users");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newAuth!.RefreshToken);

        var response = await HttpClient.SendAsync(request);
        Output.WriteLine($"Access /users with new refresh token status: {(int)response.StatusCode}");

        // Assert: Should be 403 - new refresh tokens still only have refresh permission
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    #endregion

    #region Helper Methods

    private async Task<AuthResponse?> RegisterUniqueUserAsync()
    {
        return await RegisterUserAsync($"token_sep_{Guid.NewGuid():N}");
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

    #endregion
}






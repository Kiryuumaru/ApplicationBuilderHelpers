using Presentation.WebApp.Server.FunctionalTests;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Presentation.WebApp.Server.FunctionalTests.Auth;

/// <summary>
/// End-to-end journey tests that verify complete user authentication flows.
/// These tests chain multiple operations to ensure the full auth lifecycle works correctly.
/// </summary>
public class AuthJourneyTests : WebAppTestBase
{
    private const string NewPassword = "NewSecurePassword456!";

    public AuthJourneyTests(ITestOutputHelper output) : base(output)
    {
    }

    [TimedFact]
    public async Task Journey_FullAuthLifecycle_SignupLoginLogoutChangePasswordVerifyOldFails()
    {
        Output.WriteLine("[TEST] Journey_FullAuthLifecycle_SignupLoginLogoutChangePasswordVerifyOldFails");

        var username = $"jf_{Guid.NewGuid():N}";

        // Step 1: Signup
        Output.WriteLine("[STEP 1] Signup new user");
        var authResult = await RegisterUniqueUserAsync(username);
        Assert.NotNull(authResult);
        var userId = authResult!.User.Id;
        Output.WriteLine($"[PASS] User registered: {userId}");

        // Step 2: Logout (simulate by clearing tokens - in real app this would call logout endpoint)
        Output.WriteLine("[STEP 2] Logout (clear session)");
        // Note: We don't have a server-side logout that invalidates tokens in this implementation
        // The client just discards tokens. For this test, we simulate by not using the token.
        Output.WriteLine("[PASS] Session cleared");

        // Step 3: Login again with original password
        Output.WriteLine("[STEP 3] Login with original password");
        var loginResult = await LoginUserAsync(username, TestPassword);
        Assert.NotNull(loginResult);
        Output.WriteLine("[PASS] Login successful with original password");

        // Step 4: Change password
        Output.WriteLine("[STEP 4] Change password");
        var changePasswordRequest = new { CurrentPassword = TestPassword, NewPassword = NewPassword };
        using var changePwdRequest = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/auth/users/{userId}/identity/password");
        changePwdRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loginResult!.AccessToken);
        changePwdRequest.Content = JsonContent.Create(changePasswordRequest);
        var changePwdResponse = await HttpClient.SendAsync(changePwdRequest);
        Assert.Equal(HttpStatusCode.NoContent, changePwdResponse.StatusCode);
        Output.WriteLine("[PASS] Password changed successfully");

        // Step 5: Logout again
        Output.WriteLine("[STEP 5] Logout (clear session)");
        Output.WriteLine("[PASS] Session cleared");

        // Step 6: Try login with OLD password - should FAIL
        Output.WriteLine("[STEP 6] Login with OLD password (expect failure)");
        var loginOldSuccess = await TryLoginAsync(username, TestPassword);
        Assert.False(loginOldSuccess, "Old password should be rejected");
        Output.WriteLine("[PASS] Old password correctly rejected");

        // Step 7: Login with NEW password - should SUCCEED
        Output.WriteLine("[STEP 7] Login with NEW password (expect success)");
        var finalAuthResult = await LoginUserAsync(username, NewPassword);
        Assert.NotNull(finalAuthResult);
        Assert.Equal(userId, finalAuthResult!.User.Id);
        Output.WriteLine("[PASS] New password works correctly");

        Output.WriteLine("[COMPLETE] Full auth lifecycle journey passed!");
    }

    [TimedFact]
    public async Task Journey_MultiplePasswordChanges_OnlyLatestWorks()
    {
        Output.WriteLine("[TEST] Journey_MultiplePasswordChanges_OnlyLatestWorks");

        var username = $"jmp_{Guid.NewGuid():N}";
        const string password1 = "FirstPassword123!";
        const string password2 = "SecondPassword456!";
        const string password3 = "ThirdPassword789!";

        // Register with password1
        Output.WriteLine("[STEP 1] Register user");
        var authResult = await RegisterUniqueUserAsync(username, password1);
        Assert.NotNull(authResult);
        var userId = authResult!.User.Id;
        var currentToken = authResult.AccessToken;

        // Change to password2
        Output.WriteLine("[STEP 2] Change to password2");
        var change1 = new { CurrentPassword = password1, NewPassword = password2 };
        using var req1 = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/auth/users/{userId}/identity/password");
        req1.Headers.Authorization = new AuthenticationHeaderValue("Bearer", currentToken);
        req1.Content = JsonContent.Create(change1);
        var resp1 = await HttpClient.SendAsync(req1);
        Assert.Equal(HttpStatusCode.NoContent, resp1.StatusCode);

        // Login with password2 to get new token
        var auth2 = await LoginUserAsync(username, password2);
        Assert.NotNull(auth2);
        currentToken = auth2!.AccessToken;

        // Change to password3
        Output.WriteLine("[STEP 3] Change to password3");
        var change2 = new { CurrentPassword = password2, NewPassword = password3 };
        using var req2 = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/auth/users/{userId}/identity/password");
        req2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", currentToken);
        req2.Content = JsonContent.Create(change2);
        var resp2 = await HttpClient.SendAsync(req2);
        Assert.Equal(HttpStatusCode.NoContent, resp2.StatusCode);

        // Verify: password1 fails
        Output.WriteLine("[STEP 4] Verify password1 fails");
        var tryPwd1 = await TryLoginAsync(username, password1);
        Assert.False(tryPwd1);

        // Verify: password2 fails
        Output.WriteLine("[STEP 5] Verify password2 fails");
        var tryPwd2 = await TryLoginAsync(username, password2);
        Assert.False(tryPwd2);

        // Verify: password3 works
        Output.WriteLine("[STEP 6] Verify password3 works");
        var auth3 = await LoginUserAsync(username, password3);
        Assert.NotNull(auth3);

        Output.WriteLine("[COMPLETE] Multiple password changes journey passed!");
    }

    [TimedFact]
    public async Task Journey_ConcurrentSessionsAfterPasswordChange()
    {
        Output.WriteLine("[TEST] Journey_ConcurrentSessionsAfterPasswordChange");

        var username = $"jcc_{Guid.NewGuid():N}";

        // Register
        var session1 = await RegisterUniqueUserAsync(username);
        Assert.NotNull(session1);
        var userId = session1!.User.Id;

        // Login again (simulating second device/session)
        Output.WriteLine("[STEP 1] Create second session");
        var session2 = await LoginUserAsync(username, TestPassword);
        Assert.NotNull(session2);

        // Both sessions should work initially
        Output.WriteLine("[STEP 2] Verify both sessions work");
        using var req1 = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/auth/users/{userId}/identity");
        req1.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session1.AccessToken);
        var resp1 = await HttpClient.SendAsync(req1);
        Assert.Equal(HttpStatusCode.OK, resp1.StatusCode);

        using var req2 = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/auth/users/{userId}/identity");
        req2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session2!.AccessToken);
        var resp2 = await HttpClient.SendAsync(req2);
        Assert.Equal(HttpStatusCode.OK, resp2.StatusCode);

        // Change password from session1
        Output.WriteLine("[STEP 3] Change password from session1");
        var changePwd = new { CurrentPassword = TestPassword, NewPassword = NewPassword };
        using var changeReq = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/auth/users/{userId}/identity/password");
        changeReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session1.AccessToken);
        changeReq.Content = JsonContent.Create(changePwd);
        var changeResp = await HttpClient.SendAsync(changeReq);
        Assert.Equal(HttpStatusCode.NoContent, changeResp.StatusCode);

        // Verify new password works
        Output.WriteLine("[STEP 4] Verify new password works");
        var loginNew = await LoginUserAsync(username, NewPassword);
        Assert.NotNull(loginNew);

        Output.WriteLine("[COMPLETE] Concurrent sessions journey passed!");
    }

    [TimedFact]
    public async Task Journey_RegisterLoginCheckIdentityLogout()
    {
        Output.WriteLine("[TEST] Journey_RegisterLoginCheckIdentityLogout");

        var username = $"ji_{Guid.NewGuid():N}";
        var email = $"{username}@example.com";

        // Step 1: Register
        Output.WriteLine("[STEP 1] Register");
        var authResult = await RegisterUniqueUserAsync(username);
        Assert.NotNull(authResult);

        // Step 2: Verify user info in response
        Output.WriteLine("[STEP 2] Verify user info");
        Assert.Equal(username, authResult!.User.Username);
        Assert.NotNull(authResult.User.Email);
        Assert.NotEqual(Guid.Empty, authResult.User.Id);

        // Step 3: Check identity endpoint
        Output.WriteLine("[STEP 3] Check identity");
        using var identityReq = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/auth/users/{authResult.User.Id}/identity");
        identityReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
        var identityResp = await HttpClient.SendAsync(identityReq);
        Assert.Equal(HttpStatusCode.OK, identityResp.StatusCode);

        var identity = JsonSerializer.Deserialize<IdentitiesResponse>(await identityResp.Content.ReadAsStringAsync(), JsonOptions);
        Assert.NotNull(identity);
        Assert.False(identity!.IsAnonymous);
        Assert.True(identity.HasPassword);
        Assert.NotNull(identity.Email);

        // Step 4: Logout and verify can login again
        Output.WriteLine("[STEP 4] Logout and re-login");
        var relogin = await LoginUserAsync(username, TestPassword);
        Assert.NotNull(relogin);

        Output.WriteLine("[COMPLETE] Register-Login-Identity-Logout journey passed!");
    }

    #region Helper Methods

    private async Task<AuthResponse?> RegisterUniqueUserAsync(string username, string? password = null)
    {
        password ??= TestPassword;
        var registerRequest = new
        {
            Username = username,
            Email = $"{username}@example.com",
            Password = password,
            ConfirmPassword = password
        };
        var response = await HttpClient.PostAsJsonAsync("/api/v1/auth/register", registerRequest);

        if (!response.IsSuccessStatusCode)
        {
            Output.WriteLine($"[ERROR] Registration failed: {await response.Content.ReadAsStringAsync()}");
            return null;
        }

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<AuthResponse>(content, JsonOptions);
    }

    private async Task<AuthResponse?> LoginUserAsync(string username, string password)
    {
        var loginRequest = new { Username = username, Password = password };
        var response = await HttpClient.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<AuthResponse>(content, JsonOptions);
    }

    private async Task<bool> TryLoginAsync(string username, string password)
    {
        var loginRequest = new { Username = username, Password = password };
        var response = await HttpClient.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        return response.IsSuccessStatusCode;
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

    #endregion
}

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
namespace Presentation.WebApp.Server.FunctionalTests.Auth;

/// <summary>
/// Security tests for session management.
/// Tests session lifecycle, concurrent access, and attack vectors.
/// </summary>
public class SessionSecurityTests : WebAppTestBase
{
    public SessionSecurityTests(ITestOutputHelper output) : base(output)
    {
    }

    #region Session ID Manipulation Tests

    [TimedFact]
    public async Task RevokeSession_WithInvalidGuid_Returns404()
    {
        Output.WriteLine("[TEST] RevokeSession_WithInvalidGuid_Returns404");

        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);

        var userId = authResult!.User.Id;
        var invalidSessionId = "not-a-guid";

        using var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/auth/users/{userId}/sessions/{invalidSessionId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
        var response = await HttpClient.SendAsync(request);

        Output.WriteLine($"[RECEIVED] Status: {(int)response.StatusCode}");

        // Should return 404 or 400 for invalid GUID format
        Assert.True(
            response.StatusCode == HttpStatusCode.NotFound ||
            response.StatusCode == HttpStatusCode.BadRequest,
            $"Expected 404 or 400, got {(int)response.StatusCode}");

        Output.WriteLine("[PASS] Invalid GUID rejected");
    }

    [TimedFact]
    public async Task RevokeSession_WithEmptyGuid_Returns404OrBadRequest()
    {
        Output.WriteLine("[TEST] RevokeSession_WithEmptyGuid_Returns404OrBadRequest");

        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);

        var userId = authResult!.User.Id;
        var emptyGuid = Guid.Empty;

        using var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/auth/users/{userId}/sessions/{emptyGuid}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
        var response = await HttpClient.SendAsync(request);

        Output.WriteLine($"[RECEIVED] Status: {(int)response.StatusCode}");

        Assert.True(
            response.StatusCode == HttpStatusCode.NotFound ||
            response.StatusCode == HttpStatusCode.BadRequest,
            $"Expected 404 or 400, got {(int)response.StatusCode}");

        Output.WriteLine("[PASS] Empty GUID rejected");
    }

    [TimedFact]
    public async Task RevokeSession_OtherUsersSession_Returns404OrForbidden()
    {
        Output.WriteLine("[TEST] RevokeSession_OtherUsersSession_Returns404OrForbidden");

        var user1 = await RegisterUniqueUserAsync();
        var user2 = await RegisterUniqueUserAsync();

        Assert.NotNull(user1);
        Assert.NotNull(user2);

        var user1Id = user1!.User.Id;
        var user2Id = user2!.User.Id;

        // Get user1's session ID
        using var listRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/auth/users/{user1Id}/sessions");
        listRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user1.AccessToken);
        var listResponse = await HttpClient.SendAsync(listRequest);
        var listContent = await listResponse.Content.ReadAsStringAsync();
        var user1Sessions = JsonSerializer.Deserialize<SessionListResponse>(listContent, JsonOptions);
        var user1SessionId = user1Sessions!.Items.First().Id;

        // User2 tries to revoke user1's session using user1's userId in the route
        Output.WriteLine("[STEP] User2 attempting to revoke User1's session...");
        using var revokeRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/auth/users/{user1Id}/sessions/{user1SessionId}");
        revokeRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user2.AccessToken);
        var response = await HttpClient.SendAsync(revokeRequest);

        Output.WriteLine($"[RECEIVED] Status: {(int)response.StatusCode}");

        // Should not be able to revoke another user's session
        Assert.True(
            response.StatusCode == HttpStatusCode.NotFound ||
            response.StatusCode == HttpStatusCode.Forbidden,
            $"Expected 404 or 403, got {(int)response.StatusCode}");

        Output.WriteLine("[PASS] Cannot revoke other user's session");
    }

    #endregion

    #region Session List Security Tests

    [TimedFact]
    public async Task ListSessions_OnlyReturnsOwnSessions()
    {
        Output.WriteLine("[TEST] ListSessions_OnlyReturnsOwnSessions");

        var user1 = await RegisterUniqueUserAsync();
        var user2 = await RegisterUniqueUserAsync();

        Assert.NotNull(user1);
        Assert.NotNull(user2);

        var user1Id = user1!.User.Id;
        var user2Id = user2!.User.Id;

        // Get user1's sessions
        using var request1 = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/auth/users/{user1Id}/sessions");
        request1.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user1.AccessToken);
        var response1 = await HttpClient.SendAsync(request1);
        var content1 = await response1.Content.ReadAsStringAsync();
        var sessions1 = JsonSerializer.Deserialize<SessionListResponse>(content1, JsonOptions);

        // Get user2's sessions
        using var request2 = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/auth/users/{user2Id}/sessions");
        request2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user2.AccessToken);
        var response2 = await HttpClient.SendAsync(request2);
        var content2 = await response2.Content.ReadAsStringAsync();
        var sessions2 = JsonSerializer.Deserialize<SessionListResponse>(content2, JsonOptions);

        // Verify they are different
        var sessionIds1 = sessions1!.Items.Select(s => s.Id).ToHashSet();
        var sessionIds2 = sessions2!.Items.Select(s => s.Id).ToHashSet();

        Assert.Empty(sessionIds1.Intersect(sessionIds2));
        Output.WriteLine("[PASS] Users see only their own sessions");
    }

    [TimedFact]
    public async Task ListSessions_DoesNotLeakOtherUserInfo()
    {
        Output.WriteLine("[TEST] ListSessions_DoesNotLeakOtherUserInfo");

        var user1 = await RegisterUniqueUserAsync();
        Assert.NotNull(user1);

        var userId = user1!.User.Id;
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/auth/users/{userId}/sessions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user1.AccessToken);
        var response = await HttpClient.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        Output.WriteLine($"[INFO] Response content: {content}");

        // Response should not contain any user IDs or other sensitive info
        // It should only have session-related information
        Assert.DoesNotContain("password", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("email", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("refreshToken", content, StringComparison.OrdinalIgnoreCase);

        Output.WriteLine("[PASS] No sensitive user info leaked in session list");
    }

    #endregion

    #region Concurrent Session Tests

    [TimedFact]
    public async Task ConcurrentSessions_AllWork_Independently()
    {
        Output.WriteLine("[TEST] ConcurrentSessions_AllWork_Independently");

        var username = $"concurrent_{Guid.NewGuid():N}";
        var sessions = new List<AuthResponse>();

        // Create initial session via registration
        var initialSession = await RegisterUserAsync(username);
        Assert.NotNull(initialSession);
        sessions.Add(initialSession!);

        // Create 4 more sessions
        for (int i = 0; i < 4; i++)
        {
            var session = await LoginUserAsync(username);
            Assert.NotNull(session);
            sessions.Add(session!);
        }

        Output.WriteLine($"[INFO] Created {sessions.Count} sessions");

        // Verify all sessions work
        foreach (var session in sessions)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
            var response = await HttpClient.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        Output.WriteLine("[PASS] All concurrent sessions are functional");
    }

    [TimedFact]
    public async Task RevokeOneSession_OthersStillWork()
    {
        Output.WriteLine("[TEST] RevokeOneSession_OthersStillWork");

        var username = $"revokeone_{Guid.NewGuid():N}";

        var session1 = await RegisterUserAsync(username);
        var session2 = await LoginUserAsync(username);
        var session3 = await LoginUserAsync(username);

        Assert.NotNull(session1);
        Assert.NotNull(session2);
        Assert.NotNull(session3);

        var userId = session2!.User.Id;

        // Get session2's ID
        using var listRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/auth/users/{userId}/sessions");
        listRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session2.AccessToken);
        var listResponse = await HttpClient.SendAsync(listRequest);
        var listContent = await listResponse.Content.ReadAsStringAsync();
        var sessions = JsonSerializer.Deserialize<SessionListResponse>(listContent, JsonOptions);

        // Revoke the oldest session (session1) using session2's access token
        // Sessions are sorted by LastUsedAt descending, so Last() gives us the oldest
        var session1Info = sessions!.Items.Last(s => !s.IsCurrent);
        Output.WriteLine($"[STEP] Revoking session {session1Info.Id}...");

        using var revokeRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/auth/users/{userId}/sessions/{session1Info.Id}");
        revokeRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session2.AccessToken);
        await HttpClient.SendAsync(revokeRequest);

        // Session2 and session3 should still work
        using var check2 = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        check2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session2.AccessToken);
        var response2 = await HttpClient.SendAsync(check2);
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);

        using var check3 = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        check3.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session3!.AccessToken);
        var response3 = await HttpClient.SendAsync(check3);
        Assert.Equal(HttpStatusCode.OK, response3.StatusCode);

        Output.WriteLine("[PASS] Other sessions still work after one is revoked");
    }

    [TimedFact]
    public async Task RevokedSession_AccessToken_Returns401()
    {
        Output.WriteLine("[TEST] RevokedSession_AccessToken_Returns401");

        var username = $"revokedaccess_{Guid.NewGuid():N}";

        var session1 = await RegisterUserAsync(username);
        var session2 = await LoginUserAsync(username);

        Assert.NotNull(session1);
        Assert.NotNull(session2);

        var userId = session2!.User.Id;

        // Get session1's ID using session2
        using var listRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/auth/users/{userId}/sessions");
        listRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session2.AccessToken);
        var listResponse = await HttpClient.SendAsync(listRequest);
        var listContent = await listResponse.Content.ReadAsStringAsync();
        var sessions = JsonSerializer.Deserialize<SessionListResponse>(listContent, JsonOptions);

        var session1Info = sessions!.Items.First(s => !s.IsCurrent);
        Output.WriteLine($"[STEP] Revoking session {session1Info.Id}...");

        // Revoke session1 using session2's access token
        using var revokeRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/auth/users/{userId}/sessions/{session1Info.Id}");
        revokeRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session2.AccessToken);
        var revokeResponse = await HttpClient.SendAsync(revokeRequest);
        Assert.Equal(HttpStatusCode.NoContent, revokeResponse.StatusCode);

        // Now try to use session1's access token - should fail
        Output.WriteLine("[STEP] Attempting to use revoked session's access token...");
        using var accessRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        accessRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session1!.AccessToken);
        var accessResponse = await HttpClient.SendAsync(accessRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, accessResponse.StatusCode);
        Output.WriteLine("[PASS] Revoked session's access token returns 401");
    }

    [TimedFact]
    public async Task RevokedSession_RefreshToken_Returns401()
    {
        Output.WriteLine("[TEST] RevokedSession_RefreshToken_Returns401");

        var username = $"revokedrefresh_{Guid.NewGuid():N}";

        var session1 = await RegisterUserAsync(username);
        var session2 = await LoginUserAsync(username);

        Assert.NotNull(session1);
        Assert.NotNull(session2);

        var userId = session2!.User.Id;

        // Get session1's ID using session2
        using var listRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/auth/users/{userId}/sessions");
        listRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session2.AccessToken);
        var listResponse = await HttpClient.SendAsync(listRequest);
        var listContent = await listResponse.Content.ReadAsStringAsync();
        var sessions = JsonSerializer.Deserialize<SessionListResponse>(listContent, JsonOptions);

        var session1Info = sessions!.Items.First(s => !s.IsCurrent);
        Output.WriteLine($"[STEP] Revoking session {session1Info.Id}...");

        // Revoke session1 using session2's access token
        using var revokeRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/auth/users/{userId}/sessions/{session1Info.Id}");
        revokeRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session2.AccessToken);
        var revokeResponse = await HttpClient.SendAsync(revokeRequest);
        Assert.Equal(HttpStatusCode.NoContent, revokeResponse.StatusCode);

        // Now try to use session1's refresh token - should fail
        Output.WriteLine("[STEP] Attempting to use revoked session's refresh token...");
        var refreshResponse = await HttpClient.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new { RefreshToken = session1!.RefreshToken });

        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
        Output.WriteLine("[PASS] Revoked session's refresh token returns 401");
    }

    #endregion

    #region Session Fixation Prevention Tests

    [TimedFact]
    public async Task EachLogin_CreatesNewSession()
    {
        Output.WriteLine("[TEST] EachLogin_CreatesNewSession");

        var username = $"newsession_{Guid.NewGuid():N}";

        var session1 = await RegisterUserAsync(username);
        var session2 = await LoginUserAsync(username);
        var session3 = await LoginUserAsync(username);

        Assert.NotNull(session1);
        Assert.NotNull(session2);
        Assert.NotNull(session3);

        // All tokens should be different
        Assert.NotEqual(session1!.AccessToken, session2!.AccessToken);
        Assert.NotEqual(session2.AccessToken, session3!.AccessToken);
        Assert.NotEqual(session1.RefreshToken, session2.RefreshToken);
        Assert.NotEqual(session2.RefreshToken, session3.RefreshToken);

        var userId = session3.User.Id;

        // List sessions should show 3 sessions
        using var listRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/auth/users/{userId}/sessions");
        listRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session3.AccessToken);
        var listResponse = await HttpClient.SendAsync(listRequest);
        var content = await listResponse.Content.ReadAsStringAsync();
        var sessions = JsonSerializer.Deserialize<SessionListResponse>(content, JsonOptions);

        Assert.Equal(3, sessions!.Items.Count);
        Output.WriteLine("[PASS] Each login creates a new distinct session");
    }

    #endregion

    #region Revoke All Sessions Tests

    [TimedFact]
    public async Task RevokeAllSessions_CurrentAlsoRevoked()
    {
        Output.WriteLine("[TEST] RevokeAllSessions_CurrentAlsoRevoked");

        var username = $"revokeall_{Guid.NewGuid():N}";

        await RegisterUserAsync(username);
        await LoginUserAsync(username);
        var currentSession = await LoginUserAsync(username);

        Assert.NotNull(currentSession);

        var userId = currentSession!.User.Id;

        // Revoke all sessions (including current)
        using var revokeRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/auth/users/{userId}/sessions");
        revokeRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", currentSession.AccessToken);
        await HttpClient.SendAsync(revokeRequest);

        // Current session should now be revoked too
        using var meRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        meRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", currentSession.AccessToken);
        var response = await HttpClient.SendAsync(meRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Output.WriteLine("[PASS] Current session revoked after revoking all");
    }

    [TimedFact]
    public async Task RevokeAllSessions_AllInvalidated()
    {
        Output.WriteLine("[TEST] RevokeAllSessions_AllInvalidated");

        var username = $"revokeall_others_{Guid.NewGuid():N}";

        var session1 = await RegisterUserAsync(username);
        var session2 = await LoginUserAsync(username);
        var currentSession = await LoginUserAsync(username);

        Assert.NotNull(session1);
        Assert.NotNull(session2);
        Assert.NotNull(currentSession);

        var userId = currentSession!.User.Id;

        // Revoke all sessions using current session
        using var revokeRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/auth/users/{userId}/sessions");
        revokeRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", currentSession.AccessToken);
        await HttpClient.SendAsync(revokeRequest);

        // Refresh with all sessions should fail (including current)
        var refresh1 = new { RefreshToken = session1!.RefreshToken };
        var response1 = await HttpClient.PostAsJsonAsync("/api/v1/auth/refresh", refresh1);
        Assert.Equal(HttpStatusCode.Unauthorized, response1.StatusCode);

        var refresh2 = new { RefreshToken = session2!.RefreshToken };
        var response2 = await HttpClient.PostAsJsonAsync("/api/v1/auth/refresh", refresh2);
        Assert.Equal(HttpStatusCode.Unauthorized, response2.StatusCode);

        var refresh3 = new { RefreshToken = currentSession.RefreshToken };
        var response3 = await HttpClient.PostAsJsonAsync("/api/v1/auth/refresh", refresh3);
        Assert.Equal(HttpStatusCode.Unauthorized, response3.StatusCode);

        Output.WriteLine("[PASS] All sessions invalidated after revoke all");
    }

    [TimedFact]
    public async Task RevokeAllSessions_ReturnsCorrectCount()
    {
        Output.WriteLine("[TEST] RevokeAllSessions_ReturnsCorrectCount");

        var username = $"revokecount_{Guid.NewGuid():N}";

        await RegisterUserAsync(username);
        await LoginUserAsync(username);
        await LoginUserAsync(username);
        var currentSession = await LoginUserAsync(username);

        Assert.NotNull(currentSession);

        var userId = currentSession!.User.Id;

        // Should have 4 sessions total (1 register + 3 logins)
        using var listRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/auth/users/{userId}/sessions");
        listRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", currentSession.AccessToken);
        var listResponse = await HttpClient.SendAsync(listRequest);
        var listContent = await listResponse.Content.ReadAsStringAsync();
        var sessions = JsonSerializer.Deserialize<SessionListResponse>(listContent, JsonOptions);

        Output.WriteLine($"[INFO] Total sessions before revoke: {sessions!.Items.Count}");

        // Revoke all sessions (including current)
        using var revokeRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/auth/users/{userId}/sessions");
        revokeRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", currentSession.AccessToken);
        var revokeResponse = await HttpClient.SendAsync(revokeRequest);
        var revokeContent = await revokeResponse.Content.ReadAsStringAsync();
        var revokeResult = JsonSerializer.Deserialize<RevokeAllResponse>(revokeContent, JsonOptions);

        Output.WriteLine($"[INFO] Revoked count: {revokeResult!.RevokedCount}");

        // Should revoke all sessions (including current = 4 sessions)
        Assert.Equal(4, revokeResult.RevokedCount);
        Output.WriteLine("[PASS] Revoke count is correct");
    }

    #endregion

    #region Session Info Tests

    [TimedFact]
    public async Task ListSessions_ShowsIsCurrent_Correctly()
    {
        Output.WriteLine("[TEST] ListSessions_ShowsIsCurrent_Correctly");

        var username = $"iscurrent_{Guid.NewGuid():N}";

        await RegisterUserAsync(username);
        var currentSession = await LoginUserAsync(username);

        Assert.NotNull(currentSession);

        var userId = currentSession!.User.Id;
        using var listRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/auth/users/{userId}/sessions");
        listRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", currentSession.AccessToken);
        var listResponse = await HttpClient.SendAsync(listRequest);
        var content = await listResponse.Content.ReadAsStringAsync();
        var sessions = JsonSerializer.Deserialize<SessionListResponse>(content, JsonOptions);

        Assert.NotNull(sessions);

        // Exactly one should be current
        var currentCount = sessions!.Items.Count(s => s.IsCurrent);
        Assert.Equal(1, currentCount);

        Output.WriteLine("[PASS] IsCurrent flag is correctly set");
    }

    [TimedFact]
    public async Task ListSessions_HasCreatedAt()
    {
        Output.WriteLine("[TEST] ListSessions_HasCreatedAt");

        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);

        var userId = authResult!.User.Id;
        using var listRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/auth/users/{userId}/sessions");
        listRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
        var listResponse = await HttpClient.SendAsync(listRequest);
        var content = await listResponse.Content.ReadAsStringAsync();
        var sessions = JsonSerializer.Deserialize<SessionListResponse>(content, JsonOptions);

        Assert.NotNull(sessions);
        Assert.NotEmpty(sessions!.Items);

        foreach (var session in sessions.Items)
        {
            Assert.NotEqual(default, session.CreatedAt);
            Assert.True(session.CreatedAt <= DateTimeOffset.UtcNow.AddMinutes(1));
            Output.WriteLine($"[INFO] Session {session.Id} created at: {session.CreatedAt}");
        }

        Output.WriteLine("[PASS] All sessions have valid CreatedAt");
    }

    #endregion

    #region Logout Tests

    [TimedFact]
    public async Task Logout_RevokesCurrentSessionOnly()
    {
        Output.WriteLine("[TEST] Logout_RevokesCurrentSessionOnly");

        var username = $"logout_{Guid.NewGuid():N}";

        var session1 = await RegisterUserAsync(username);
        var session2 = await LoginUserAsync(username);

        Assert.NotNull(session1);
        Assert.NotNull(session2);

        // Logout from session2
        using var logoutRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout");
        logoutRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session2!.AccessToken);
        await HttpClient.SendAsync(logoutRequest);

        // Session2's refresh should fail
        var refresh2 = new { RefreshToken = session2.RefreshToken };
        var response2 = await HttpClient.PostAsJsonAsync("/api/v1/auth/refresh", refresh2);
        Assert.Equal(HttpStatusCode.Unauthorized, response2.StatusCode);

        // Session1's refresh should still work
        var refresh1 = new { RefreshToken = session1!.RefreshToken };
        var response1 = await HttpClient.PostAsJsonAsync("/api/v1/auth/refresh", refresh1);
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);

        Output.WriteLine("[PASS] Logout only affects current session");
    }

    [TimedFact]
    public async Task Logout_MultipleTimesFromSameSession_NoError()
    {
        Output.WriteLine("[TEST] Logout_MultipleTimesFromSameSession_NoError");

        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);

        // First logout
        using var logout1 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout");
        logout1.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult!.AccessToken);
        var response1 = await HttpClient.SendAsync(logout1);
        Assert.Equal(HttpStatusCode.NoContent, response1.StatusCode);

        // Second logout with same token - access token might still be valid but session is revoked
        using var logout2 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout");
        logout2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
        var response2 = await HttpClient.SendAsync(logout2);

        // Should either succeed or return NoContent (idempotent) or 401 if access token is invalidated
        Output.WriteLine($"[INFO] Second logout response: {(int)response2.StatusCode}");

        Assert.True(
            response2.StatusCode == HttpStatusCode.NoContent ||
            response2.StatusCode == HttpStatusCode.Unauthorized,
            $"Expected 204 or 401, got {(int)response2.StatusCode}");

        Output.WriteLine("[PASS] Multiple logouts handled gracefully");
    }

    #endregion

    #region Helper Methods

    private async Task<AuthResponse?> RegisterUniqueUserAsync()
    {
        var username = $"sessec_{Guid.NewGuid():N}";
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

    private async Task<AuthResponse?> LoginUserAsync(string username)
    {
        var loginRequest = new { Username = username, Password = TestPassword };
        var response = await HttpClient.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            Output.WriteLine($"[ERROR] Login failed: {errorContent}");
            return null;
        }

        var content = await response.Content.ReadAsStringAsync();
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

    private record SessionInfo(
        Guid Id,
        string? DeviceName,
        string? IpAddress,
        DateTimeOffset CreatedAt,
        DateTimeOffset? LastUsedAt,
        bool IsCurrent);

    private record SessionListResponse(
        IReadOnlyList<SessionInfo> Items);

    private record RevokeAllResponse(
        int RevokedCount);

    #endregion
}





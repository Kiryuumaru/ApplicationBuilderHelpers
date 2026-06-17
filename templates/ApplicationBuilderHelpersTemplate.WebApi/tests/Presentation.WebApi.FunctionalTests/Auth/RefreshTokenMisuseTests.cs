using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Presentation.WebApi.FunctionalTests.Fixtures;

namespace Presentation.WebApi.FunctionalTests.Auth;

/// <summary>
/// Deep token misuse tests that strengthen confidence in RBAC-based token separation.
///
/// These tests focus on:
/// - Cross-user header confusion at /auth/refresh (Authorization header vs refresh token in body)
/// - Using refresh tokens as bearer tokens on other protected endpoints
/// </summary>
public sealed class RefreshTokenMisuseTests : WebApiTestBase
{
    public RefreshTokenMisuseTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async Task Refresh_WithBodyTokenOfUserA_AndAuthorizationHeaderOfUserB_IssuesTokensForUserA()
    {
        Output.WriteLine("[TEST] Refresh_WithBodyTokenOfUserA_AndAuthorizationHeaderOfUserB_IssuesTokensForUserA");

        // Arrange: two distinct users
        var userA = await RegisterUserAsync();
        Assert.NotNull(userA);
        Assert.NotNull(userA!.User);

        var userB = await RegisterUserAsync();
        Assert.NotNull(userB);

        // Act: call /auth/refresh with A's refresh token in body, but B's access token in Authorization header
        using var refreshReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        refreshReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userB!.AccessToken);
        refreshReq.Content = JsonContent.Create(new { RefreshToken = userA.RefreshToken });

        var refreshResp = await HttpClient.SendAsync(refreshReq);
        Output.WriteLine($"[RECEIVED] Refresh status: {(int)refreshResp.StatusCode} {refreshResp.StatusCode}");
        Assert.Equal(HttpStatusCode.OK, refreshResp.StatusCode);

        var refreshContent = await refreshResp.Content.ReadAsStringAsync();
        var refreshed = JsonSerializer.Deserialize<AuthResponse>(refreshContent, JsonOptions);
        Assert.NotNull(refreshed);
        Assert.False(string.IsNullOrWhiteSpace(refreshed!.AccessToken));

        // Assert: the issued access token belongs to user A (prove via /auth/me)
        using var meReq = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        meReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", refreshed.AccessToken);

        var meResp = await HttpClient.SendAsync(meReq);
        Output.WriteLine($"[RECEIVED] /me status: {(int)meResp.StatusCode} {meResp.StatusCode}");
        Assert.Equal(HttpStatusCode.OK, meResp.StatusCode);

        var meContent = await meResp.Content.ReadAsStringAsync();
        var me = JsonSerializer.Deserialize<MeResponse>(meContent, JsonOptions);
        Assert.NotNull(me);
        Assert.Equal(userA.User!.Id, me!.Id);

        Output.WriteLine("[PASS] Refresh uses body token identity, not Authorization header");
    }

    [Fact]
    public async Task RefreshToken_AsBearerToken_CannotAccessUserSessions_Returns403()
    {
        Output.WriteLine("[TEST] RefreshToken_AsBearerToken_CannotAccessUserSessions_Returns403");

        var auth = await RegisterUserAsync();
        Assert.NotNull(auth);
        Assert.NotNull(auth!.User);

        // Act: use refresh token as bearer token on sessions endpoint
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/auth/users/{auth.User!.Id}/sessions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.RefreshToken);

        var response = await HttpClient.SendAsync(request);
        Output.WriteLine($"[RECEIVED] Status: {(int)response.StatusCode} {response.StatusCode}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Output.WriteLine("[PASS] Refresh token cannot be used as bearer token for sessions");
    }

    private sealed class MeResponse
    {
        public Guid Id { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public IReadOnlyCollection<string>? Roles { get; set; }
        public IReadOnlyCollection<string>? Permissions { get; set; }
        public bool IsAnonymous { get; set; }
    }
}

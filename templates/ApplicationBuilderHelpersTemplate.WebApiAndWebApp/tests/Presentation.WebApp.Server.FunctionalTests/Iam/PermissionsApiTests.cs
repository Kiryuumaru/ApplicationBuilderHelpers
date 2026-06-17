using Presentation.WebApp.Server.FunctionalTests;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Presentation.WebApp.Server.FunctionalTests.Iam;

/// <summary>
/// Functional tests for IAM Permissions API endpoints.
/// Tests permission grant and revoke operations.
/// </summary>
public class PermissionsApiTests : WebAppTestBase
{
    // Use unique usernames per test run to avoid conflicts
    private readonly string _testUsername = $"testuser_{Guid.NewGuid():N}";

    public PermissionsApiTests(ITestOutputHelper output) : base(output)
    {
    }

    #region Permission Grant/Revoke Tests

    [TimedFact]
    public async Task GrantPermission_AsRegularUser_Returns403()
    {
        Output.WriteLine("[TEST] GrantPermission_AsRegularUser_Returns403");

        var userAuth = await RegisterAndGetTokenAsync(_testUsername);
        Assert.NotNull(userAuth);
        var userId = userAuth!.User!.Id;

        var grantRequest = new { UserId = userId, PermissionIdentifier = "api:iam:users:read", Description = "Test grant" };

        Output.WriteLine("[STEP] POST /api/v1/iam/permissions/grant...");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/iam/permissions/grant");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userAuth.AccessToken);
        request.Content = JsonContent.Create(grantRequest);
        var response = await HttpClient.SendAsync(request);

        Output.WriteLine($"[RECEIVED] Status: {(int)response.StatusCode} {response.StatusCode}");

        // Regular users don't have iam:permissions:grant permission
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Output.WriteLine("[PASS] Cannot grant permissions without admin access");
    }

    [TimedFact]
    public async Task RevokePermission_AsRegularUser_Returns403()
    {
        Output.WriteLine("[TEST] RevokePermission_AsRegularUser_Returns403");

        var userAuth = await RegisterAndGetTokenAsync(_testUsername);
        Assert.NotNull(userAuth);
        var userId = userAuth!.User!.Id;

        var revokeRequest = new { UserId = userId, PermissionIdentifier = "api:iam:users:read" };

        Output.WriteLine("[STEP] POST /api/v1/iam/permissions/revoke...");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/iam/permissions/revoke");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userAuth.AccessToken);
        request.Content = JsonContent.Create(revokeRequest);
        var response = await HttpClient.SendAsync(request);

        Output.WriteLine($"[RECEIVED] Status: {(int)response.StatusCode} {response.StatusCode}");

        // Regular users don't have iam:permissions:revoke permission
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Output.WriteLine("[PASS] Cannot revoke permissions without admin access");
    }

    #endregion

    #region Helper Methods

    private async Task<AuthResponse?> RegisterAndGetTokenAsync(string? username = null)
    {
        username ??= $"user_{Guid.NewGuid():N}";

        var registerRequest = new
        {
            Username = username,
            Password = TestPassword,
            ConfirmPassword = TestPassword,
            Email = $"{username}@test.com"
        };

        var registerResponse = await HttpClient.PostAsJsonAsync("/api/v1/auth/register", registerRequest);
        
        if (registerResponse.StatusCode == HttpStatusCode.Conflict)
        {
            // User already exists, just login
            var loginReq = new { Username = username, Password = TestPassword };
            registerResponse = await HttpClient.PostAsJsonAsync("/api/v1/auth/login", loginReq);
        }

        if (!registerResponse.IsSuccessStatusCode)
        {
            var error = await registerResponse.Content.ReadAsStringAsync();
            Output.WriteLine($"[ERROR] Registration failed: {error}");
            return null;
        }

        var content = await registerResponse.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<AuthResponse>(content, JsonOptions);
    }

    #endregion

    #region Response Types

    private sealed class AuthResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public string TokenType { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }
        public UserInfo? User { get; set; }
    }

    private sealed class UserInfo
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? Email { get; set; }
        public IReadOnlyCollection<string>? Roles { get; set; }
        public IReadOnlyCollection<string>? Permissions { get; set; }
    }

    #endregion
}








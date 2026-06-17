using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Presentation.WebApp.Server.FunctionalTests.Iam;

/// <summary>
/// Functional tests for IAM Roles API endpoints.
/// Tests role assignment and removal operations.
/// </summary>
public class RolesApiTests : WebAppTestBase
{
    // Use unique usernames per test run to avoid conflicts
    private readonly string _testUsername = $"testuser_{Guid.NewGuid():N}";

    public RolesApiTests(ITestOutputHelper output) : base(output)
    {
    }

    #region Role Management Tests

    [TimedFact]
    public async Task AssignRole_AsRegularUser_Returns403()
    {
        Output.WriteLine("[TEST] AssignRole_AsRegularUser_Returns403");

        var userAuth = await RegisterAndGetTokenAsync(_testUsername);
        Assert.NotNull(userAuth);
        var userId = userAuth!.User!.Id;

        var roleRequest = new { UserId = userId, RoleCode = "ADMIN" };

        Output.WriteLine("[STEP] POST /api/v1/iam/roles/assign...");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/iam/roles/assign");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userAuth.AccessToken);
        request.Content = JsonContent.Create(roleRequest);
        var response = await HttpClient.SendAsync(request);

        Output.WriteLine($"[RECEIVED] Status: {(int)response.StatusCode} {response.StatusCode}");

        // Regular users don't have iam:roles:assign permission
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Output.WriteLine("[PASS] Cannot assign roles without admin access");
    }

    [TimedFact]
    public async Task RemoveRole_AsRegularUser_Returns403()
    {
        Output.WriteLine("[TEST] RemoveRole_AsRegularUser_Returns403");

        var userAuth = await RegisterAndGetTokenAsync(_testUsername);
        Assert.NotNull(userAuth);
        var userId = userAuth!.User!.Id;
        var roleId = Guid.Parse("00000000-0000-0000-0000-000000000002"); // User role

        var roleRequest = new { UserId = userId, RoleId = roleId };

        Output.WriteLine("[STEP] POST /api/v1/iam/roles/remove...");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/iam/roles/remove");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userAuth.AccessToken);
        request.Content = JsonContent.Create(roleRequest);
        var response = await HttpClient.SendAsync(request);

        Output.WriteLine($"[RECEIVED] Status: {(int)response.StatusCode} {response.StatusCode}");

        // Regular users don't have iam:roles:remove permission
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Output.WriteLine("[PASS] Cannot remove roles without admin access");
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








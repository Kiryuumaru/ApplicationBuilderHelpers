using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
namespace Presentation.WebApp.Server.FunctionalTests.Iam;

/// <summary>
/// Deliberately "messy" policy states that should still behave deterministically.
/// These tests focus on mixed allow/deny across multiple roles and direct grants.
/// </summary>
public class PermissionChaosTests : WebAppTestBase
{
    public PermissionChaosTests(ITestOutputHelper output) : base(output)
    {
    }

    [TimedFact]
    public async Task MixedRoles_AllowAndDeny_ForSamePermission_DenyWins()
    {
        Output.WriteLine("[TEST] MixedRoles_AllowAndDeny_ForSamePermission_DenyWins");

        var adminAuth = await CreateAdminUserAsync();
        Assert.NotNull(adminAuth);

        // Roles
        var uniqueId = Guid.NewGuid().ToString("N")[..12];
        var allowRoleCode = $"ALLOW_READ_{uniqueId}";
        var denyRoleCode = $"DENY_READ_{uniqueId}";

        await CreateRoleAsync(adminAuth!, allowRoleCode, "Allow Read", new[]
        {
            new ScopeTemplateRequest("allow", "api:iam:users:read")
        });

        await CreateRoleAsync(adminAuth!, denyRoleCode, "Deny Read", new[]
        {
            new ScopeTemplateRequest("deny", "api:iam:users:read")
        });

        // Users
        var user = await RegisterAndGetTokenAsync();
        Assert.NotNull(user);
        var userId = user!.User!.Id;

        var target = await RegisterAndGetTokenAsync();
        Assert.NotNull(target);
        var targetUserId = target!.User!.Id;

        // First: assign allow role only and prove it grants access
        await AssignRoleAsync(adminAuth!, userId, allowRoleCode);

        var userWithAllowRole = await LoginAsync(user.User!.Username!, TestPassword);
        Assert.NotNull(userWithAllowRole);

        using (var allowReq = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/iam/users/{targetUserId}"))
        {
            allowReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userWithAllowRole!.AccessToken);
            var allowResp = await HttpClient.SendAsync(allowReq);
            Output.WriteLine($"[RECEIVED] With allow role: {(int)allowResp.StatusCode} {allowResp.StatusCode}");
            Assert.Equal(HttpStatusCode.OK, allowResp.StatusCode);
        }

        // Then: add deny role and prove deny takes precedence
        await AssignRoleAsync(adminAuth!, userId, denyRoleCode);

        var userWithAllowAndDenyRoles = await LoginAsync(user.User!.Username!, TestPassword);
        Assert.NotNull(userWithAllowAndDenyRoles);

        using (var denyReq = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/iam/users/{targetUserId}"))
        {
            denyReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userWithAllowAndDenyRoles!.AccessToken);
            var denyResp = await HttpClient.SendAsync(denyReq);
            Output.WriteLine($"[RECEIVED] With allow+deny roles: {(int)denyResp.StatusCode} {denyResp.StatusCode}");
            Assert.Equal(HttpStatusCode.Forbidden, denyResp.StatusCode);
        }

        Output.WriteLine("[PASS] Deny role wins over allow role");
    }

    [TimedFact]
    public async Task RoleDeny_BeatsDirectAllow_ForSameEndpoint()
    {
        Output.WriteLine("[TEST] RoleDeny_BeatsDirectAllow_ForSameEndpoint");

        var adminAuth = await CreateAdminUserAsync();
        Assert.NotNull(adminAuth);

        var uniqueId = Guid.NewGuid().ToString("N")[..12];
        var denyRoleCode = $"DENY_READ_ALL_{uniqueId}";

        await CreateRoleAsync(adminAuth!, denyRoleCode, "Deny All Reads", new[]
        {
            new ScopeTemplateRequest("deny", "api:iam:users:read")
        });

        var user = await RegisterAndGetTokenAsync();
        Assert.NotNull(user);
        var userId = user!.User!.Id;

        var target = await RegisterAndGetTokenAsync();
        Assert.NotNull(target);
        var targetUserId = target!.User!.Id;

        // First: add a direct allow grant for this specific target and prove it works
        var grantRequest = new
        {
            UserId = userId,
            PermissionIdentifier = $"api:iam:users:read;userId={targetUserId}",
            IsAllow = true,
            Description = "Direct allow for specific target"
        };

        using (var grantReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/iam/permissions/grant"))
        {
            grantReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminAuth!.AccessToken);
            grantReq.Content = JsonContent.Create(grantRequest);
            var grantResp = await HttpClient.SendAsync(grantReq);
            Assert.Equal(HttpStatusCode.NoContent, grantResp.StatusCode);
        }

        var userWithDirectAllow = await LoginAsync(user.User!.Username!, TestPassword);
        Assert.NotNull(userWithDirectAllow);

        using (var allowReq = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/iam/users/{targetUserId}"))
        {
            allowReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userWithDirectAllow!.AccessToken);
            var allowResp = await HttpClient.SendAsync(allowReq);
            Output.WriteLine($"[RECEIVED] With direct allow: {(int)allowResp.StatusCode} {allowResp.StatusCode}");
            Assert.Equal(HttpStatusCode.OK, allowResp.StatusCode);
        }

        // Then: assign deny role and prove it overrides the direct allow
        await AssignRoleAsync(adminAuth!, userId, denyRoleCode);

        var userWithDirectAllowAndDenyRole = await LoginAsync(user.User!.Username!, TestPassword);
        Assert.NotNull(userWithDirectAllowAndDenyRole);

        using (var denyReq = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/iam/users/{targetUserId}"))
        {
            denyReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userWithDirectAllowAndDenyRole!.AccessToken);
            var denyResp = await HttpClient.SendAsync(denyReq);
            Output.WriteLine($"[RECEIVED] With direct allow + deny role: {(int)denyResp.StatusCode} {denyResp.StatusCode}");
            Assert.Equal(HttpStatusCode.Forbidden, denyResp.StatusCode);
        }

        Output.WriteLine("[PASS] Role deny beats direct allow");
    }

    private async Task CreateRoleAsync(LocalAuthResponse adminAuth, string code, string name, ScopeTemplateRequest[] scopeTemplates)
    {
        var request = new
        {
            Code = code,
            Name = name,
            Description = "Test role",
            ScopeTemplates = scopeTemplates.Select(s => new { Type = s.Type, PermissionPath = s.PermissionPath }).ToArray()
        };

        using var createReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/iam/roles");
        createReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminAuth.AccessToken);
        createReq.Content = JsonContent.Create(request);

        var resp = await HttpClient.SendAsync(createReq);
        Output.WriteLine($"[RECEIVED] Create role {code}: {(int)resp.StatusCode} {resp.StatusCode}");
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
    }

    private async Task AssignRoleAsync(LocalAuthResponse adminAuth, Guid userId, string roleCode)
    {
        var request = new { UserId = userId, RoleCode = roleCode };

        using var assignReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/iam/roles/assign");
        assignReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminAuth.AccessToken);
        assignReq.Content = JsonContent.Create(request);

        var resp = await HttpClient.SendAsync(assignReq);
        Output.WriteLine($"[RECEIVED] Assign role {roleCode}: {(int)resp.StatusCode} {resp.StatusCode}");
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
    }

    private async Task<LocalAuthResponse?> CreateAdminUserAsync()
    {
        var username = $"admin_{Guid.NewGuid():N}";

        var createAdminRequest = new
        {
            Username = username,
            Email = $"{username}@test.com",
            Password = TestPassword
        };

        Output.WriteLine($"[HELPER] Creating admin user: {username}");
        var response = await HttpClient.PostAsJsonAsync("/api/v1/devtools/create-admin", createAdminRequest);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            Output.WriteLine($"[ERROR] Create admin failed: {error}");
            return null;
        }

        return await LoginAsync(username, TestPassword);
    }

    private async Task<LocalAuthResponse?> RegisterAndGetTokenAsync()
    {
        var username = $"user_{Guid.NewGuid():N}";

        var registerRequest = new
        {
            Username = username,
            Password = TestPassword,
            ConfirmPassword = TestPassword,
            Email = $"{username}@test.com"
        };

        var response = await HttpClient.PostAsJsonAsync("/api/v1/auth/register", registerRequest);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            Output.WriteLine($"[ERROR] Registration failed: {error}");
            return null;
        }

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<LocalAuthResponse>(content, JsonOptions);
    }

    private new async Task<LocalAuthResponse?> LoginAsync(string username, string password)
    {
        var loginRequest = new { Username = username, Password = password };

        var response = await HttpClient.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            Output.WriteLine($"[ERROR] Login failed: {error}");
            return null;
        }

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<LocalAuthResponse>(content, JsonOptions);
    }

    private sealed class LocalAuthResponse
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
        public bool IsAnonymous { get; set; }
    }

    private sealed record ScopeTemplateRequest(string Type, string PermissionPath);
}





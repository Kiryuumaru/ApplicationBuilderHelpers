using Presentation.WebApp.Server.FunctionalTests;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
namespace Presentation.WebApp.Server.FunctionalTests.Iam;

/// <summary>
/// Security tests for IAM API endpoints.
/// Tests access control, self-escalation prevention, and cross-user access restrictions.
/// </summary>
public class IamSecurityTests : WebAppTestBase
{
    public IamSecurityTests(ITestOutputHelper output) : base(output)
    {
    }

    #region Self-Escalation Prevention Tests

    [TimedFact]
    public async Task User_CannotAssignRoleToSelf()
    {
        Output.WriteLine("[TEST] User_CannotAssignRoleToSelf");

        // Register a user
        var userAuth = await RegisterAndGetTokenAsync();
        Assert.NotNull(userAuth);
        var userId = userAuth!.User!.Id;

        // Attempt to assign ADMIN role to self
        var roleRequest = new { UserId = userId, RoleCode = "ADMIN" };

        Output.WriteLine($"[STEP] User {userId} attempting to assign ADMIN role to themselves...");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/iam/roles/assign");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userAuth.AccessToken);
        request.Content = JsonContent.Create(roleRequest);
        var response = await HttpClient.SendAsync(request);

        Output.WriteLine($"[RECEIVED] Status: {(int)response.StatusCode} {response.StatusCode}");

        // Should be forbidden - users can't assign roles to themselves
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Output.WriteLine("[PASS] User cannot assign role to themselves");
    }

    [TimedFact]
    public async Task User_CannotRemoveRoleFromSelf()
    {
        Output.WriteLine("[TEST] User_CannotRemoveRoleFromSelf");

        var userAuth = await RegisterAndGetTokenAsync();
        Assert.NotNull(userAuth);
        var userId = userAuth!.User!.Id;

        // User role ID (standard user role)
        var userRoleId = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var roleRequest = new { UserId = userId, RoleId = userRoleId };

        Output.WriteLine($"[STEP] User {userId} attempting to remove USER role from themselves...");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/iam/roles/remove");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userAuth.AccessToken);
        request.Content = JsonContent.Create(roleRequest);
        var response = await HttpClient.SendAsync(request);

        Output.WriteLine($"[RECEIVED] Status: {(int)response.StatusCode} {response.StatusCode}");

        // Should be forbidden
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Output.WriteLine("[PASS] User cannot remove role from themselves");
    }

    [TimedFact]
    public async Task User_CannotGrantPermissionToSelf()
    {
        Output.WriteLine("[TEST] User_CannotGrantPermissionToSelf");

        var userAuth = await RegisterAndGetTokenAsync();
        Assert.NotNull(userAuth);
        var userId = userAuth!.User!.Id;

        // Attempt to grant admin permission to self
        var grantRequest = new
        {
            UserId = userId,
            PermissionIdentifier = "api:iam:users:_write",
            Description = "Self-escalation attempt"
        };

        Output.WriteLine($"[STEP] User {userId} attempting to grant admin permission to themselves...");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/iam/permissions/grant");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userAuth.AccessToken);
        request.Content = JsonContent.Create(grantRequest);
        var response = await HttpClient.SendAsync(request);

        Output.WriteLine($"[RECEIVED] Status: {(int)response.StatusCode} {response.StatusCode}");

        // Should be forbidden
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Output.WriteLine("[PASS] User cannot grant permission to themselves");
    }

    [TimedFact]
    public async Task User_CannotRevokePermissionFromSelf()
    {
        Output.WriteLine("[TEST] User_CannotRevokePermissionFromSelf");

        var userAuth = await RegisterAndGetTokenAsync();
        Assert.NotNull(userAuth);
        var userId = userAuth!.User!.Id;

        var revokeRequest = new
        {
            UserId = userId,
            PermissionIdentifier = "api:auth:me:_write"
        };

        Output.WriteLine($"[STEP] User {userId} attempting to revoke permission from themselves...");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/iam/permissions/revoke");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userAuth.AccessToken);
        request.Content = JsonContent.Create(revokeRequest);
        var response = await HttpClient.SendAsync(request);

        Output.WriteLine($"[RECEIVED] Status: {(int)response.StatusCode} {response.StatusCode}");

        // Should be forbidden
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Output.WriteLine("[PASS] User cannot revoke permission from themselves");
    }

    #endregion

    #region Cross-User Access Prevention Tests

    [TimedFact]
    public async Task User_CannotAccessOtherUserInfo()
    {
        Output.WriteLine("[TEST] User_CannotAccessOtherUserInfo");

        // Register two users
        var user1Auth = await RegisterAndGetTokenAsync();
        var user2Auth = await RegisterAndGetTokenAsync();
        Assert.NotNull(user1Auth);
        Assert.NotNull(user2Auth);

        var user2Id = user2Auth!.User!.Id;

        Output.WriteLine($"[STEP] User 1 attempting to access User 2's info ({user2Id})...");
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/iam/users/{user2Id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user1Auth!.AccessToken);
        var response = await HttpClient.SendAsync(request);

        Output.WriteLine($"[RECEIVED] Status: {(int)response.StatusCode} {response.StatusCode}");

        // Should be forbidden - users can't access other users' info
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Output.WriteLine("[PASS] User cannot access other user's info");
    }

    [TimedFact]
    public async Task User_CannotUpdateOtherUser()
    {
        Output.WriteLine("[TEST] User_CannotUpdateOtherUser");

        var user1Auth = await RegisterAndGetTokenAsync();
        var user2Auth = await RegisterAndGetTokenAsync();
        Assert.NotNull(user1Auth);
        Assert.NotNull(user2Auth);

        var user2Id = user2Auth!.User!.Id;
        var updateRequest = new { Email = "hacked@evil.com" };

        Output.WriteLine($"[STEP] User 1 attempting to update User 2's profile ({user2Id})...");
        using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/iam/users/{user2Id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user1Auth!.AccessToken);
        request.Content = JsonContent.Create(updateRequest);
        var response = await HttpClient.SendAsync(request);

        Output.WriteLine($"[RECEIVED] Status: {(int)response.StatusCode} {response.StatusCode}");

        // Should be forbidden
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Output.WriteLine("[PASS] User cannot update other user's profile");
    }

    [TimedFact]
    public async Task User_CannotDeleteOtherUser()
    {
        Output.WriteLine("[TEST] User_CannotDeleteOtherUser");

        var user1Auth = await RegisterAndGetTokenAsync();
        var user2Auth = await RegisterAndGetTokenAsync();
        Assert.NotNull(user1Auth);
        Assert.NotNull(user2Auth);

        var user2Id = user2Auth!.User!.Id;

        Output.WriteLine($"[STEP] User 1 attempting to delete User 2 ({user2Id})...");
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/iam/users/{user2Id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user1Auth!.AccessToken);
        var response = await HttpClient.SendAsync(request);

        Output.WriteLine($"[RECEIVED] Status: {(int)response.StatusCode} {response.StatusCode}");

        // Should be forbidden
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Output.WriteLine("[PASS] User cannot delete other user");
    }

    [TimedFact]
    public async Task User_CannotViewOtherUserPermissions()
    {
        Output.WriteLine("[TEST] User_CannotViewOtherUserPermissions");

        var user1Auth = await RegisterAndGetTokenAsync();
        var user2Auth = await RegisterAndGetTokenAsync();
        Assert.NotNull(user1Auth);
        Assert.NotNull(user2Auth);

        var user2Id = user2Auth!.User!.Id;

        Output.WriteLine($"[STEP] User 1 attempting to view User 2's permissions ({user2Id})...");
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/iam/users/{user2Id}/permissions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user1Auth!.AccessToken);
        var response = await HttpClient.SendAsync(request);

        Output.WriteLine($"[RECEIVED] Status: {(int)response.StatusCode} {response.StatusCode}");

        // Should be forbidden
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Output.WriteLine("[PASS] User cannot view other user's permissions");
    }

    [TimedFact]
    public async Task User_CannotAssignRoleToOtherUser()
    {
        Output.WriteLine("[TEST] User_CannotAssignRoleToOtherUser");

        var user1Auth = await RegisterAndGetTokenAsync();
        var user2Auth = await RegisterAndGetTokenAsync();
        Assert.NotNull(user1Auth);
        Assert.NotNull(user2Auth);

        var user2Id = user2Auth!.User!.Id;
        var roleRequest = new { UserId = user2Id, RoleCode = "ADMIN" };

        Output.WriteLine($"[STEP] User 1 attempting to assign ADMIN role to User 2 ({user2Id})...");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/iam/roles/assign");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user1Auth!.AccessToken);
        request.Content = JsonContent.Create(roleRequest);
        var response = await HttpClient.SendAsync(request);

        Output.WriteLine($"[RECEIVED] Status: {(int)response.StatusCode} {response.StatusCode}");

        // Should be forbidden
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Output.WriteLine("[PASS] User cannot assign role to other user");
    }

    [TimedFact]
    public async Task User_CannotGrantPermissionToOtherUser()
    {
        Output.WriteLine("[TEST] User_CannotGrantPermissionToOtherUser");

        var user1Auth = await RegisterAndGetTokenAsync();
        var user2Auth = await RegisterAndGetTokenAsync();
        Assert.NotNull(user1Auth);
        Assert.NotNull(user2Auth);

        var user2Id = user2Auth!.User!.Id;
        var grantRequest = new
        {
            UserId = user2Id,
            PermissionIdentifier = "api:iam:users:_write",
            Description = "Unauthorized grant attempt"
        };

        Output.WriteLine($"[STEP] User 1 attempting to grant permission to User 2 ({user2Id})...");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/iam/permissions/grant");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user1Auth!.AccessToken);
        request.Content = JsonContent.Create(grantRequest);
        var response = await HttpClient.SendAsync(request);

        Output.WriteLine($"[RECEIVED] Status: {(int)response.StatusCode} {response.StatusCode}");

        // Should be forbidden
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Output.WriteLine("[PASS] User cannot grant permission to other user");
    }

    #endregion

    #region IAM Endpoint Access Control Tests

    [TimedFact]
    public async Task UnauthenticatedUser_CannotAccessIamEndpoints()
    {
        Output.WriteLine("[TEST] UnauthenticatedUser_CannotAccessIamEndpoints");

        var endpoints = new[]
        {
            ("GET", "/api/v1/iam/users"),
            ("GET", $"/api/v1/iam/users/{Guid.NewGuid()}"),
            ("PUT", $"/api/v1/iam/users/{Guid.NewGuid()}"),
            ("DELETE", $"/api/v1/iam/users/{Guid.NewGuid()}"),
            ("GET", $"/api/v1/iam/users/{Guid.NewGuid()}/permissions"),
            ("POST", "/api/v1/iam/roles/assign"),
            ("POST", "/api/v1/iam/roles/remove"),
            ("POST", "/api/v1/iam/permissions/grant"),
            ("POST", "/api/v1/iam/permissions/revoke")
        };

        foreach (var (method, endpoint) in endpoints)
        {
            Output.WriteLine($"[STEP] {method} {endpoint} without authentication...");
            using var request = new HttpRequestMessage(new HttpMethod(method), endpoint);

            // Add content for POST/PUT requests
            if (method is "POST" or "PUT")
            {
                request.Content = JsonContent.Create(new { });
            }

            var response = await HttpClient.SendAsync(request);

            Output.WriteLine($"[RECEIVED] Status: {(int)response.StatusCode} {response.StatusCode}");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        Output.WriteLine("[PASS] All IAM endpoints require authentication");
    }

    [TimedFact]
    public async Task RegularUser_CannotListAllUsers()
    {
        Output.WriteLine("[TEST] RegularUser_CannotListAllUsers");

        var userAuth = await RegisterAndGetTokenAsync();
        Assert.NotNull(userAuth);

        Output.WriteLine("[STEP] GET /api/v1/iam/users as regular user...");
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/iam/users");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userAuth!.AccessToken);
        var response = await HttpClient.SendAsync(request);

        Output.WriteLine($"[RECEIVED] Status: {(int)response.StatusCode} {response.StatusCode}");

        // Regular users don't have api:iam:users:list permission
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Output.WriteLine("[PASS] Regular user cannot list all users");
    }

    [TimedFact]
    public async Task RegularUser_CanAccessOwnInfo()
    {
        Output.WriteLine("[TEST] RegularUser_CanAccessOwnInfo");

        var userAuth = await RegisterAndGetTokenAsync();
        Assert.NotNull(userAuth);
        var userId = userAuth!.User!.Id;

        Output.WriteLine($"[STEP] GET /api/v1/iam/users/{userId} as the owner...");
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/iam/users/{userId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userAuth.AccessToken);
        var response = await HttpClient.SendAsync(request);

        Output.WriteLine($"[RECEIVED] Status: {(int)response.StatusCode} {response.StatusCode}");

        // Users should be able to access their own info via userId-scoped permission
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<UserResponse>(content, JsonOptions);
        Assert.NotNull(result);
        Assert.Equal(userId, result!.Id);

        Output.WriteLine("[PASS] User can access their own info");
    }

    [TimedFact]
    public async Task RegularUser_CanViewOwnPermissions()
    {
        Output.WriteLine("[TEST] RegularUser_CanViewOwnPermissions");

        var userAuth = await RegisterAndGetTokenAsync();
        Assert.NotNull(userAuth);
        var userId = userAuth!.User!.Id;

        Output.WriteLine($"[STEP] GET /api/v1/iam/users/{userId}/permissions as the owner...");
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/iam/users/{userId}/permissions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userAuth.AccessToken);
        var response = await HttpClient.SendAsync(request);

        Output.WriteLine($"[RECEIVED] Status: {(int)response.StatusCode} {response.StatusCode}");

        // Users should be able to view their own permissions
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Output.WriteLine($"[RECEIVED] Body: {content}");

        var result = JsonSerializer.Deserialize<PermissionsResponse>(content, JsonOptions);
        Assert.NotNull(result);
        Assert.Equal(userId, result!.UserId);

        Output.WriteLine("[PASS] User can view their own permissions");
    }

    [TimedFact]
    public async Task RegularUser_CanUpdateOwnProfile()
    {
        Output.WriteLine("[TEST] RegularUser_CanUpdateOwnProfile");

        var userAuth = await RegisterAndGetTokenAsync();
        Assert.NotNull(userAuth);
        var userId = userAuth!.User!.Id;
        var newEmail = $"updated_{Guid.NewGuid():N}@test.com";

        var updateRequest = new { Email = newEmail };

        Output.WriteLine($"[STEP] PUT /api/v1/iam/users/{userId} to update own email...");
        using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/iam/users/{userId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userAuth.AccessToken);
        request.Content = JsonContent.Create(updateRequest);
        var response = await HttpClient.SendAsync(request);

        Output.WriteLine($"[RECEIVED] Status: {(int)response.StatusCode} {response.StatusCode}");

        // Users should be able to update their own profile
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<UserResponse>(content, JsonOptions);
        Assert.NotNull(result);
        Assert.Equal(newEmail, result!.Email);

        Output.WriteLine("[PASS] User can update their own profile");
    }

    #endregion

    #region Privilege Escalation Attempt Tests

    [TimedFact]
    public async Task User_CannotEscalateViaRoleAssignment()
    {
        Output.WriteLine("[TEST] User_CannotEscalateViaRoleAssignment");

        var userAuth = await RegisterAndGetTokenAsync();
        Assert.NotNull(userAuth);
        var userId = userAuth!.User!.Id;

        // Try multiple admin-level roles
        var adminRoles = new[] { "ADMIN", "SUPER_ADMIN", "SYSTEM", "ROOT" };

        foreach (var roleCode in adminRoles)
        {
            var roleRequest = new { UserId = userId, RoleCode = roleCode };

            Output.WriteLine($"[STEP] Attempting to assign {roleCode} role to self...");
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/iam/roles/assign");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userAuth.AccessToken);
            request.Content = JsonContent.Create(roleRequest);
            var response = await HttpClient.SendAsync(request);

            Output.WriteLine($"[RECEIVED] Status: {(int)response.StatusCode} {response.StatusCode}");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        Output.WriteLine("[PASS] User cannot escalate privileges via role assignment");
    }

    [TimedFact]
    public async Task User_CannotEscalateViaPermissionGrant()
    {
        Output.WriteLine("[TEST] User_CannotEscalateViaPermissionGrant");

        var userAuth = await RegisterAndGetTokenAsync();
        Assert.NotNull(userAuth);
        var userId = userAuth!.User!.Id;

        // Try granting admin-level permissions
        var adminPermissions = new[]
        {
            "api:iam:users:_write",
            "api:iam:roles:assign",
            "api:iam:permissions:grant",
            "*",
            "api:*"
        };

        foreach (var permission in adminPermissions)
        {
            var grantRequest = new
            {
                UserId = userId,
                PermissionIdentifier = permission,
                Description = "Escalation attempt"
            };

            Output.WriteLine($"[STEP] Attempting to grant '{permission}' to self...");
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/iam/permissions/grant");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userAuth.AccessToken);
            request.Content = JsonContent.Create(grantRequest);
            var response = await HttpClient.SendAsync(request);

            Output.WriteLine($"[RECEIVED] Status: {(int)response.StatusCode} {response.StatusCode}");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        Output.WriteLine("[PASS] User cannot escalate privileges via permission grant");
    }

    #endregion

    #region Helper Methods

    private async Task<AuthResponse?> RegisterAndGetTokenAsync()
    {
        var username = $"user_{Guid.NewGuid():N}";

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

    private sealed class UserResponse
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? Email { get; set; }
        public bool EmailConfirmed { get; set; }
        public string? PhoneNumber { get; set; }
        public bool PhoneNumberConfirmed { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public bool LockoutEnabled { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public int AccessFailedCount { get; set; }
        public DateTimeOffset Created { get; set; }
        public IReadOnlyCollection<Guid> RoleIds { get; set; } = [];
    }

    private sealed class PermissionsResponse
    {
        public Guid UserId { get; set; }
        public IReadOnlyCollection<string> Permissions { get; set; } = [];
    }

    #endregion
}





using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
namespace Presentation.WebApp.Server.FunctionalTests.Iam;

/// <summary>
/// Functional tests for Allow/Deny permission grant types.
/// Tests that:
/// - Allow grants give access to users without the permission from roles
/// - Deny grants block access even when user has permission from roles
/// - Revoking grants restores role-based access
/// </summary>
public class PermissionAllowDenyTests : WebAppTestBase
{
    public PermissionAllowDenyTests(ITestOutputHelper output) : base(output)
    {
    }

    #region Allow Grant Tests

    /// <summary>
    /// Test that an Allow grant gives a user access to a protected endpoint
    /// even without having a role that grants the permission.
    /// </summary>
    [TimedFact]
    public async Task AllowGrant_GivesAccessWithoutRole()
    {
        Output.WriteLine("[TEST] AllowGrant_GivesAccessWithoutRole");

        var adminAuth = await CreateAdminUserAsync();
        Assert.NotNull(adminAuth);

        // Create a regular user (no special roles)
        var regularUser = await RegisterAndGetTokenAsync();
        Assert.NotNull(regularUser);
        var regularUserId = regularUser!.User!.Id;

        // Create another user to test access against
        var targetUser = await RegisterAndGetTokenAsync();
        Assert.NotNull(targetUser);
        var targetUserId = targetUser!.User!.Id;

        // Verify regular user CANNOT access target user (no permission)
        Output.WriteLine("[STEP] Verifying user CANNOT access other user without permission...");
        using var beforeReq = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/iam/users/{targetUserId}");
        beforeReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", regularUser.AccessToken);
        var beforeResp = await HttpClient.SendAsync(beforeReq);
        Output.WriteLine($"[RECEIVED] Before grant: {(int)beforeResp.StatusCode} {beforeResp.StatusCode}");
        Assert.Equal(HttpStatusCode.Forbidden, beforeResp.StatusCode);

        // Grant Allow permission directly to the user (as admin)
        Output.WriteLine("[STEP] Granting Allow permission to user...");
        var grantRequest = new
        {
            UserId = regularUserId,
            PermissionIdentifier = $"api:iam:users:read;userId={targetUserId}",
            IsAllow = true,
            Description = "Test allow grant"
        };
        using var grantReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/iam/permissions/grant");
        grantReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminAuth!.AccessToken);
        grantReq.Content = JsonContent.Create(grantRequest);
        var grantResp = await HttpClient.SendAsync(grantReq);
        Output.WriteLine($"[RECEIVED] Grant: {(int)grantResp.StatusCode} {grantResp.StatusCode}");
        Assert.Equal(HttpStatusCode.NoContent, grantResp.StatusCode);

        // Get new token for the user (to reflect the new direct grant)
        var newUserAuth = await LoginAsync(regularUser.User!.Username!, TestPassword);
        Assert.NotNull(newUserAuth);

        // Verify user CAN now access target user
        Output.WriteLine("[STEP] Verifying user CAN access target user after Allow grant...");
        using var afterReq = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/iam/users/{targetUserId}");
        afterReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newUserAuth!.AccessToken);
        var afterResp = await HttpClient.SendAsync(afterReq);
        Output.WriteLine($"[RECEIVED] After grant: {(int)afterResp.StatusCode} {afterResp.StatusCode}");
        Assert.Equal(HttpStatusCode.OK, afterResp.StatusCode);

        Output.WriteLine("[PASS] Allow grant gives access without requiring a role");
    }

    /// <summary>
    /// Test that revoking an Allow grant removes access.
    /// </summary>
    [TimedFact]
    public async Task RevokeAllowGrant_RemovesAccess()
    {
        Output.WriteLine("[TEST] RevokeAllowGrant_RemovesAccess");

        var adminAuth = await CreateAdminUserAsync();
        Assert.NotNull(adminAuth);

        var regularUser = await RegisterAndGetTokenAsync();
        Assert.NotNull(regularUser);
        var regularUserId = regularUser!.User!.Id;

        var targetUser = await RegisterAndGetTokenAsync();
        Assert.NotNull(targetUser);
        var targetUserId = targetUser!.User!.Id;

        // Grant Allow permission
        Output.WriteLine("[STEP] Granting Allow permission...");
        var grantRequest = new
        {
            UserId = regularUserId,
            PermissionIdentifier = $"api:iam:users:read;userId={targetUserId}",
            IsAllow = true
        };
        using var grantReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/iam/permissions/grant");
        grantReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminAuth!.AccessToken);
        grantReq.Content = JsonContent.Create(grantRequest);
        await HttpClient.SendAsync(grantReq);

        // Verify access works
        var withGrantAuth = await LoginAsync(regularUser.User!.Username!, TestPassword);
        using var accessReq1 = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/iam/users/{targetUserId}");
        accessReq1.Headers.Authorization = new AuthenticationHeaderValue("Bearer", withGrantAuth!.AccessToken);
        var accessResp1 = await HttpClient.SendAsync(accessReq1);
        Assert.Equal(HttpStatusCode.OK, accessResp1.StatusCode);

        // Revoke the permission
        Output.WriteLine("[STEP] Revoking the Allow grant...");
        var revokeRequest = new
        {
            UserId = regularUserId,
            PermissionIdentifier = $"api:iam:users:read;userId={targetUserId}"
        };
        using var revokeReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/iam/permissions/revoke");
        revokeReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminAuth.AccessToken);
        revokeReq.Content = JsonContent.Create(revokeRequest);
        var revokeResp = await HttpClient.SendAsync(revokeReq);
        Output.WriteLine($"[RECEIVED] Revoke: {(int)revokeResp.StatusCode} {revokeResp.StatusCode}");
        Assert.Equal(HttpStatusCode.NoContent, revokeResp.StatusCode);

        // Get new token after revocation
        var withoutGrantAuth = await LoginAsync(regularUser.User!.Username!, TestPassword);
        Assert.NotNull(withoutGrantAuth);

        // Verify access is denied
        Output.WriteLine("[STEP] Verifying access is denied after revocation...");
        using var accessReq2 = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/iam/users/{targetUserId}");
        accessReq2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", withoutGrantAuth!.AccessToken);
        var accessResp2 = await HttpClient.SendAsync(accessReq2);
        Output.WriteLine($"[RECEIVED] After revoke: {(int)accessResp2.StatusCode} {accessResp2.StatusCode}");
        Assert.Equal(HttpStatusCode.Forbidden, accessResp2.StatusCode);

        Output.WriteLine("[PASS] Revoking Allow grant removes access");
    }

    #endregion

    #region Deny Grant Tests

    /// <summary>
    /// Test that a Deny grant blocks access even when user has permission from a role.
    /// </summary>
    [TimedFact]
    public async Task DenyGrant_BlocksAccessDespiteRole()
    {
        Output.WriteLine("[TEST] DenyGrant_BlocksAccessDespiteRole");

        var adminAuth = await CreateAdminUserAsync();
        Assert.NotNull(adminAuth);

        // Create a custom role that grants api:iam:users:read
        var uniqueId = Guid.NewGuid().ToString("N")[..12];
        var roleCode = $"READER_{uniqueId}";
        var createRoleRequest = new
        {
            Code = roleCode,
            Name = $"User Reader {uniqueId}",
            Description = "Can read any user",
            ScopeTemplates = new[]
            {
                new { Type = "allow", PermissionPath = "api:iam:users:read" }
            }
        };

        Output.WriteLine("[STEP] Creating role with api:iam:users:read permission...");
        using var createRoleReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/iam/roles");
        createRoleReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminAuth!.AccessToken);
        createRoleReq.Content = JsonContent.Create(createRoleRequest);
        var createRoleResp = await HttpClient.SendAsync(createRoleReq);
        Assert.Equal(HttpStatusCode.Created, createRoleResp.StatusCode);

        // Create regular user and target user
        var regularUser = await RegisterAndGetTokenAsync();
        Assert.NotNull(regularUser);
        var regularUserId = regularUser!.User!.Id;

        var targetUser = await RegisterAndGetTokenAsync();
        Assert.NotNull(targetUser);
        var targetUserId = targetUser!.User!.Id;

        // Assign the role to the user
        Output.WriteLine("[STEP] Assigning role to user...");
        var assignRequest = new { UserId = regularUserId, RoleCode = roleCode };
        using var assignReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/iam/roles/assign");
        assignReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminAuth.AccessToken);
        assignReq.Content = JsonContent.Create(assignRequest);
        await HttpClient.SendAsync(assignReq);

        // Get new token with role
        var userWithRole = await LoginAsync(regularUser.User!.Username!, TestPassword);
        Assert.NotNull(userWithRole);

        // Verify user CAN access target (role grants permission)
        Output.WriteLine("[STEP] Verifying user CAN access with role...");
        using var accessReq1 = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/iam/users/{targetUserId}");
        accessReq1.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userWithRole!.AccessToken);
        var accessResp1 = await HttpClient.SendAsync(accessReq1);
        Output.WriteLine($"[RECEIVED] With role: {(int)accessResp1.StatusCode} {accessResp1.StatusCode}");
        Assert.Equal(HttpStatusCode.OK, accessResp1.StatusCode);

        // Now grant a DENY for this specific target user
        Output.WriteLine("[STEP] Granting Deny for specific target user...");
        var denyRequest = new
        {
            UserId = regularUserId,
            PermissionIdentifier = $"api:iam:users:read;userId={targetUserId}",
            IsAllow = false, // DENY
            Description = "Block access to this specific user"
        };
        using var denyReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/iam/permissions/grant");
        denyReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminAuth.AccessToken);
        denyReq.Content = JsonContent.Create(denyRequest);
        var denyResp = await HttpClient.SendAsync(denyReq);
        Output.WriteLine($"[RECEIVED] Deny grant: {(int)denyResp.StatusCode} {denyResp.StatusCode}");
        Assert.Equal(HttpStatusCode.NoContent, denyResp.StatusCode);

        // Get new token with deny
        var userWithDeny = await LoginAsync(regularUser.User!.Username!, TestPassword);
        Assert.NotNull(userWithDeny);

        // Verify user CANNOT access target despite having role
        Output.WriteLine("[STEP] Verifying user CANNOT access target despite role (Deny overrides)...");
        using var accessReq2 = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/iam/users/{targetUserId}");
        accessReq2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userWithDeny!.AccessToken);
        var accessResp2 = await HttpClient.SendAsync(accessReq2);
        Output.WriteLine($"[RECEIVED] With Deny: {(int)accessResp2.StatusCode} {accessResp2.StatusCode}");
        Assert.Equal(HttpStatusCode.Forbidden, accessResp2.StatusCode);

        // Create another user to verify role still works for others
        var otherUser = await RegisterAndGetTokenAsync();
        Assert.NotNull(otherUser);
        var otherUserId = otherUser!.User!.Id;

        // Verify user CAN still access other users (deny is specific to targetUserId)
        Output.WriteLine("[STEP] Verifying user CAN still access other users (deny is specific)...");
        using var accessReq3 = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/iam/users/{otherUserId}");
        accessReq3.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userWithDeny.AccessToken);
        var accessResp3 = await HttpClient.SendAsync(accessReq3);
        Output.WriteLine($"[RECEIVED] Access other user: {(int)accessResp3.StatusCode} {accessResp3.StatusCode}");
        Assert.Equal(HttpStatusCode.OK, accessResp3.StatusCode);

        Output.WriteLine("[PASS] Deny grant blocks access despite role, but only for the specific resource");
    }

    /// <summary>
    /// Test that revoking a Deny grant restores role-based access.
    /// </summary>
    [TimedFact]
    public async Task RevokeDenyGrant_RestoresRoleAccess()
    {
        Output.WriteLine("[TEST] RevokeDenyGrant_RestoresRoleAccess");

        var adminAuth = await CreateAdminUserAsync();
        Assert.NotNull(adminAuth);

        // Create a role with read permission
        var uniqueId = Guid.NewGuid().ToString("N")[..12];
        var roleCode = $"READER_{uniqueId}";
        var createRoleRequest = new
        {
            Code = roleCode,
            Name = $"User Reader {uniqueId}",
            ScopeTemplates = new[]
            {
                new { Type = "allow", PermissionPath = "api:iam:users:read" }
            }
        };

        using var createRoleReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/iam/roles");
        createRoleReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminAuth!.AccessToken);
        createRoleReq.Content = JsonContent.Create(createRoleRequest);
        await HttpClient.SendAsync(createRoleReq);

        var regularUser = await RegisterAndGetTokenAsync();
        Assert.NotNull(regularUser);
        var regularUserId = regularUser!.User!.Id;

        var targetUser = await RegisterAndGetTokenAsync();
        Assert.NotNull(targetUser);
        var targetUserId = targetUser!.User!.Id;

        // Assign role
        var assignRequest = new { UserId = regularUserId, RoleCode = roleCode };
        using var assignReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/iam/roles/assign");
        assignReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminAuth.AccessToken);
        assignReq.Content = JsonContent.Create(assignRequest);
        await HttpClient.SendAsync(assignReq);

        // Grant Deny
        var denyRequest = new
        {
            UserId = regularUserId,
            PermissionIdentifier = $"api:iam:users:read;userId={targetUserId}",
            IsAllow = false
        };
        using var denyReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/iam/permissions/grant");
        denyReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminAuth.AccessToken);
        denyReq.Content = JsonContent.Create(denyRequest);
        await HttpClient.SendAsync(denyReq);

        // Verify denied
        var userWithDeny = await LoginAsync(regularUser.User!.Username!, TestPassword);
        using var accessReq1 = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/iam/users/{targetUserId}");
        accessReq1.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userWithDeny!.AccessToken);
        var accessResp1 = await HttpClient.SendAsync(accessReq1);
        Assert.Equal(HttpStatusCode.Forbidden, accessResp1.StatusCode);

        // Revoke the deny
        Output.WriteLine("[STEP] Revoking the Deny grant...");
        var revokeRequest = new
        {
            UserId = regularUserId,
            PermissionIdentifier = $"api:iam:users:read;userId={targetUserId}"
        };
        using var revokeReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/iam/permissions/revoke");
        revokeReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminAuth.AccessToken);
        revokeReq.Content = JsonContent.Create(revokeRequest);
        var revokeResp = await HttpClient.SendAsync(revokeReq);
        Output.WriteLine($"[RECEIVED] Revoke: {(int)revokeResp.StatusCode} {revokeResp.StatusCode}");
        Assert.Equal(HttpStatusCode.NoContent, revokeResp.StatusCode);

        // Get new token
        var userWithoutDeny = await LoginAsync(regularUser.User!.Username!, TestPassword);
        Assert.NotNull(userWithoutDeny);

        // Verify access is restored via role
        Output.WriteLine("[STEP] Verifying access is restored via role after revoking Deny...");
        using var accessReq2 = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/iam/users/{targetUserId}");
        accessReq2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userWithoutDeny!.AccessToken);
        var accessResp2 = await HttpClient.SendAsync(accessReq2);
        Output.WriteLine($"[RECEIVED] After revoke: {(int)accessResp2.StatusCode} {accessResp2.StatusCode}");
        Assert.Equal(HttpStatusCode.OK, accessResp2.StatusCode);

        Output.WriteLine("[PASS] Revoking Deny grant restores role-based access");
    }

    /// <summary>
    /// Test that a Deny grant does not apply when its parameters do not match the request,
    /// even if the user has broad access via a role.
    /// </summary>
    [TimedFact]
    public async Task DenyGrant_WithNonMatchingParameters_DoesNotOverrideRoleAllow()
    {
        Output.WriteLine("[TEST] DenyGrant_WithNonMatchingParameters_DoesNotOverrideRoleAllow");

        var adminAuth = await CreateAdminUserAsync();
        Assert.NotNull(adminAuth);

        // Create a role that grants broad read access
        var uniqueId = Guid.NewGuid().ToString("N")[..12];
        var roleCode = $"READER_{uniqueId}";
        var createRoleRequest = new
        {
            Code = roleCode,
            Name = $"User Reader {uniqueId}",
            ScopeTemplates = new[]
            {
                new { Type = "allow", PermissionPath = "api:iam:users:read" }
            }
        };

        using (var createRoleReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/iam/roles"))
        {
            createRoleReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminAuth!.AccessToken);
            createRoleReq.Content = JsonContent.Create(createRoleRequest);
            var createRoleResp = await HttpClient.SendAsync(createRoleReq);
            Assert.Equal(HttpStatusCode.Created, createRoleResp.StatusCode);
        }

        // Create user and two targets
        var regularUser = await RegisterAndGetTokenAsync();
        Assert.NotNull(regularUser);
        var regularUserId = regularUser!.User!.Id;

        var allowedTarget = await RegisterAndGetTokenAsync();
        Assert.NotNull(allowedTarget);
        var allowedTargetUserId = allowedTarget!.User!.Id;

        var deniedTarget = await RegisterAndGetTokenAsync();
        Assert.NotNull(deniedTarget);
        var deniedTargetUserId = deniedTarget!.User!.Id;

        // Assign role that grants read
        var assignRequest = new { UserId = regularUserId, RoleCode = roleCode };
        using (var assignReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/iam/roles/assign"))
        {
            assignReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminAuth.AccessToken);
            assignReq.Content = JsonContent.Create(assignRequest);
            var assignResp = await HttpClient.SendAsync(assignReq);
            Assert.Equal(HttpStatusCode.NoContent, assignResp.StatusCode);
        }

        // Grant a deny for a DIFFERENT target userId
        var denyRequest = new
        {
            UserId = regularUserId,
            PermissionIdentifier = $"api:iam:users:read;userId={deniedTargetUserId}",
            IsAllow = false,
            Description = "Block access to one specific user"
        };
        using (var denyReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/iam/permissions/grant"))
        {
            denyReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminAuth.AccessToken);
            denyReq.Content = JsonContent.Create(denyRequest);
            var denyResp = await HttpClient.SendAsync(denyReq);
            Assert.Equal(HttpStatusCode.NoContent, denyResp.StatusCode);
        }

        var userWithRoleAndDeny = await LoginAsync(regularUser.User!.Username!, TestPassword);
        Assert.NotNull(userWithRoleAndDeny);

        // Verify: can still access a different user (deny params do not match)
        using (var accessAllowedReq = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/iam/users/{allowedTargetUserId}"))
        {
            accessAllowedReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userWithRoleAndDeny!.AccessToken);
            var resp = await HttpClient.SendAsync(accessAllowedReq);
            Output.WriteLine($"[RECEIVED] Access allowed target: {(int)resp.StatusCode} {resp.StatusCode}");
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        }

        // Verify: cannot access the denied user (deny params match)
        using (var accessDeniedReq = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/iam/users/{deniedTargetUserId}"))
        {
            accessDeniedReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userWithRoleAndDeny.AccessToken);
            var resp = await HttpClient.SendAsync(accessDeniedReq);
            Output.WriteLine($"[RECEIVED] Access denied target: {(int)resp.StatusCode} {resp.StatusCode}");
            Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
        }

        Output.WriteLine("[PASS] Deny with non-matching parameters does not override role allow");
    }

    /// <summary>
    /// Test that having an Allow grant for one permission does not grant access to other permissions.
    /// This locks in that the requested permission must match at least one allow directive.
    /// </summary>
    [TimedFact]
    public async Task AllowGrant_ForDifferentPermission_DoesNotGrantAccessToOtherPermission()
    {
        Output.WriteLine("[TEST] AllowGrant_ForDifferentPermission_DoesNotGrantAccessToOtherPermission");

        var adminAuth = await CreateAdminUserAsync();
        Assert.NotNull(adminAuth);

        var regularUser = await RegisterAndGetTokenAsync();
        Assert.NotNull(regularUser);
        var regularUserId = regularUser!.User!.Id;

        var targetUser = await RegisterAndGetTokenAsync();
        Assert.NotNull(targetUser);
        var targetUserId = targetUser!.User!.Id;

        // Grant allow ONLY for list users
        var grantListRequest = new
        {
            UserId = regularUserId,
            PermissionIdentifier = "api:iam:users:list",
            IsAllow = true,
            Description = "Allow listing users only"
        };
        using (var grantReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/iam/permissions/grant"))
        {
            grantReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminAuth!.AccessToken);
            grantReq.Content = JsonContent.Create(grantListRequest);
            var grantResp = await HttpClient.SendAsync(grantReq);
            Assert.Equal(HttpStatusCode.NoContent, grantResp.StatusCode);
        }

        // Re-login to embed the direct allow
        var userWithListAllow = await LoginAsync(regularUser.User!.Username!, TestPassword);
        Assert.NotNull(userWithListAllow);

        // Verify list endpoint is allowed
        using (var listReq = new HttpRequestMessage(HttpMethod.Get, "/api/v1/iam/users"))
        {
            listReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userWithListAllow!.AccessToken);
            var listResp = await HttpClient.SendAsync(listReq);
            Output.WriteLine($"[RECEIVED] List users: {(int)listResp.StatusCode} {listResp.StatusCode}");
            Assert.Equal(HttpStatusCode.OK, listResp.StatusCode);
        }

        // Verify reading another user's info is still forbidden (permission mismatch)
        using (var readReq = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/iam/users/{targetUserId}"))
        {
            readReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userWithListAllow.AccessToken);
            var readResp = await HttpClient.SendAsync(readReq);
            Output.WriteLine($"[RECEIVED] Read other user: {(int)readResp.StatusCode} {readResp.StatusCode}");
            Assert.Equal(HttpStatusCode.Forbidden, readResp.StatusCode);
        }

        Output.WriteLine("[PASS] Allow for one permission does not grant other permissions");
    }

    #endregion

    #region Advanced Scenarios

    /// <summary>
    /// Test that a global Deny overrides all role-based Allow permissions.
    /// </summary>
    [TimedFact]
    public async Task GlobalDeny_OverridesAllRolePermissions()
    {
        Output.WriteLine("[TEST] GlobalDeny_OverridesAllRolePermissions");

        var adminAuth = await CreateAdminUserAsync();
        Assert.NotNull(adminAuth);

        // Create a role with broad read permission
        var uniqueId = Guid.NewGuid().ToString("N")[..12];
        var roleCode = $"GLOBAL_READER_{uniqueId}";
        var createRoleRequest = new
        {
            Code = roleCode,
            Name = $"Global Reader {uniqueId}",
            ScopeTemplates = new[]
            {
                new { Type = "allow", PermissionPath = "api:iam:users:read" }
            }
        };

        using var createRoleReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/iam/roles");
        createRoleReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminAuth!.AccessToken);
        createRoleReq.Content = JsonContent.Create(createRoleRequest);
        await HttpClient.SendAsync(createRoleReq);

        var regularUser = await RegisterAndGetTokenAsync();
        Assert.NotNull(regularUser);
        var regularUserId = regularUser!.User!.Id;

        // Assign role
        var assignRequest = new { UserId = regularUserId, RoleCode = roleCode };
        using var assignReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/iam/roles/assign");
        assignReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminAuth.AccessToken);
        assignReq.Content = JsonContent.Create(assignRequest);
        await HttpClient.SendAsync(assignReq);

        // Create multiple target users
        var targetUser1 = await RegisterAndGetTokenAsync();
        var targetUser2 = await RegisterAndGetTokenAsync();
        Assert.NotNull(targetUser1);
        Assert.NotNull(targetUser2);

        // Verify can access both
        var userWithRole = await LoginAsync(regularUser.User!.Username!, TestPassword);
        using var access1 = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/iam/users/{targetUser1!.User!.Id}");
        access1.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userWithRole!.AccessToken);
        Assert.Equal(HttpStatusCode.OK, (await HttpClient.SendAsync(access1)).StatusCode);

        // Grant a GLOBAL deny (no userId parameter means all users)
        Output.WriteLine("[STEP] Granting global Deny for api:iam:users:read...");
        var globalDenyRequest = new
        {
            UserId = regularUserId,
            PermissionIdentifier = "api:iam:users:read", // No userId = global
            IsAllow = false,
            Description = "Block ALL user reads"
        };
        using var globalDenyReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/iam/permissions/grant");
        globalDenyReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminAuth.AccessToken);
        globalDenyReq.Content = JsonContent.Create(globalDenyRequest);
        var globalDenyResp = await HttpClient.SendAsync(globalDenyReq);
        Output.WriteLine($"[RECEIVED] Global Deny: {(int)globalDenyResp.StatusCode} {globalDenyResp.StatusCode}");
        Assert.Equal(HttpStatusCode.NoContent, globalDenyResp.StatusCode);

        // Get new token
        var userWithGlobalDeny = await LoginAsync(regularUser.User!.Username!, TestPassword);
        Assert.NotNull(userWithGlobalDeny);

        // Verify CANNOT access ANY user
        Output.WriteLine("[STEP] Verifying user CANNOT access any user with global Deny...");
        using var access2a = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/iam/users/{targetUser1.User!.Id}");
        access2a.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userWithGlobalDeny!.AccessToken);
        var resp2a = await HttpClient.SendAsync(access2a);
        Output.WriteLine($"[RECEIVED] Access user1: {(int)resp2a.StatusCode}");
        Assert.Equal(HttpStatusCode.Forbidden, resp2a.StatusCode);

        using var access2b = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/iam/users/{targetUser2!.User!.Id}");
        access2b.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userWithGlobalDeny.AccessToken);
        var resp2b = await HttpClient.SendAsync(access2b);
        Output.WriteLine($"[RECEIVED] Access user2: {(int)resp2b.StatusCode}");
        Assert.Equal(HttpStatusCode.Forbidden, resp2b.StatusCode);

        Output.WriteLine("[PASS] Global Deny blocks access to all resources of that type");
    }

    /// <summary>
    /// Test that direct permission grants require re-login to take effect.
    /// Design note: Unlike role scope changes (which are resolved at runtime from DB),
    /// direct permission grants are baked into the JWT token for performance.
    /// This means users must re-login to receive updated permission grants.
    /// </summary>
    [TimedFact]
    public async Task PermissionGrants_RequireReLoginToTakeEffect()
    {
        Output.WriteLine("[TEST] PermissionGrants_RequireReLoginToTakeEffect");
        Output.WriteLine("Testing that direct permission grants require re-login (they are baked into JWT)");

        var adminAuth = await CreateAdminUserAsync();
        Assert.NotNull(adminAuth);

        var regularUser = await RegisterAndGetTokenAsync();
        Assert.NotNull(regularUser);
        var regularUserId = regularUser!.User!.Id;

        var targetUser = await RegisterAndGetTokenAsync();
        Assert.NotNull(targetUser);
        var targetUserId = targetUser!.User!.Id;

        // Get the user's original token
        var originalToken = regularUser.AccessToken;

        // Verify cannot access initially
        Output.WriteLine("[STEP] Verifying CANNOT access with original token...");
        using var access1 = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/iam/users/{targetUserId}");
        access1.Headers.Authorization = new AuthenticationHeaderValue("Bearer", originalToken);
        var resp1 = await HttpClient.SendAsync(access1);
        Assert.Equal(HttpStatusCode.Forbidden, resp1.StatusCode);

        // Grant Allow permission
        Output.WriteLine("[STEP] Granting Allow permission...");
        var grantRequest = new
        {
            UserId = regularUserId,
            PermissionIdentifier = $"api:iam:users:read;userId={targetUserId}",
            IsAllow = true
        };
        using var grantReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/iam/permissions/grant");
        grantReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminAuth!.AccessToken);
        grantReq.Content = JsonContent.Create(grantRequest);
        await HttpClient.SendAsync(grantReq);

        // Using SAME token, verify access is STILL DENIED (grant is in DB but not in token)
        Output.WriteLine("[STEP] Verifying STILL CANNOT access using SAME token (grant not in token yet)...");
        using var access2 = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/iam/users/{targetUserId}");
        access2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", originalToken);
        var resp2 = await HttpClient.SendAsync(access2);
        Output.WriteLine($"[RECEIVED] After grant (same token): {(int)resp2.StatusCode}");
        Assert.Equal(HttpStatusCode.Forbidden, resp2.StatusCode);

        // Re-login to get a new token with the grant
        Output.WriteLine("[STEP] Re-logging in to get new token with grant...");
        var newAuth = await LoginAsync(regularUser.User!.Username!, TestPassword);
        Assert.NotNull(newAuth);

        // Using NEW token, verify access IS granted
        Output.WriteLine("[STEP] Verifying CAN access using NEW token (after re-login)...");
        using var access3 = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/iam/users/{targetUserId}");
        access3.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newAuth!.AccessToken);
        var resp3 = await HttpClient.SendAsync(access3);
        Output.WriteLine($"[RECEIVED] After re-login: {(int)resp3.StatusCode}");
        Assert.Equal(HttpStatusCode.OK, resp3.StatusCode);

        Output.WriteLine("[PASS] Permission grants require re-login to take effect (by design)");
    }

    /// <summary>
    /// Test precedence: specific Deny should override broad Allow from role.
    /// </summary>
    [TimedFact]
    public async Task SpecificDeny_OverridesBroadAllow()
    {
        Output.WriteLine("[TEST] SpecificDeny_OverridesBroadAllow");

        var adminAuth = await CreateAdminUserAsync();
        Assert.NotNull(adminAuth);

        // Create a role with broad permission
        var uniqueId = Guid.NewGuid().ToString("N")[..12];
        var roleCode = $"BROAD_{uniqueId}";
        var createRoleRequest = new
        {
            Code = roleCode,
            Name = $"Broad Access {uniqueId}",
            ScopeTemplates = new[]
            {
                new { Type = "allow", PermissionPath = "api:iam:users:read" } // Broad - all users
            }
        };

        using var createRoleReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/iam/roles");
        createRoleReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminAuth!.AccessToken);
        createRoleReq.Content = JsonContent.Create(createRoleRequest);
        await HttpClient.SendAsync(createRoleReq);

        var regularUser = await RegisterAndGetTokenAsync();
        var blockedUser = await RegisterAndGetTokenAsync();
        var allowedUser = await RegisterAndGetTokenAsync();
        Assert.NotNull(regularUser);
        Assert.NotNull(blockedUser);
        Assert.NotNull(allowedUser);

        var regularUserId = regularUser!.User!.Id;
        var blockedUserId = blockedUser!.User!.Id;
        var allowedUserId = allowedUser!.User!.Id;

        // Assign broad role
        var assignRequest = new { UserId = regularUserId, RoleCode = roleCode };
        using var assignReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/iam/roles/assign");
        assignReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminAuth.AccessToken);
        assignReq.Content = JsonContent.Create(assignRequest);
        await HttpClient.SendAsync(assignReq);

        // Grant specific Deny for blocked user
        Output.WriteLine("[STEP] Granting specific Deny for blocked user...");
        var denyRequest = new
        {
            UserId = regularUserId,
            PermissionIdentifier = $"api:iam:users:read;userId={blockedUserId}",
            IsAllow = false
        };
        using var denyReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/iam/permissions/grant");
        denyReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminAuth.AccessToken);
        denyReq.Content = JsonContent.Create(denyRequest);
        await HttpClient.SendAsync(denyReq);

        // Get token
        var userAuth = await LoginAsync(regularUser.User!.Username!, TestPassword);
        Assert.NotNull(userAuth);

        // Verify: CAN access allowed user (broad role applies)
        Output.WriteLine("[STEP] Verifying CAN access non-blocked user...");
        using var accessAllowed = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/iam/users/{allowedUserId}");
        accessAllowed.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userAuth!.AccessToken);
        var respAllowed = await HttpClient.SendAsync(accessAllowed);
        Output.WriteLine($"[RECEIVED] Access allowed user: {(int)respAllowed.StatusCode}");
        Assert.Equal(HttpStatusCode.OK, respAllowed.StatusCode);

        // Verify: CANNOT access blocked user (specific deny overrides)
        Output.WriteLine("[STEP] Verifying CANNOT access blocked user...");
        using var accessBlocked = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/iam/users/{blockedUserId}");
        accessBlocked.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userAuth.AccessToken);
        var respBlocked = await HttpClient.SendAsync(accessBlocked);
        Output.WriteLine($"[RECEIVED] Access blocked user: {(int)respBlocked.StatusCode}");
        Assert.Equal(HttpStatusCode.Forbidden, respBlocked.StatusCode);

        Output.WriteLine("[PASS] Specific Deny correctly overrides broad Allow from role");
    }

    #endregion

    #region Helper Methods

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

    #endregion

    #region Response Types

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
        public string? Username { get; set; }
        public string? Email { get; set; }
        public IReadOnlyCollection<string>? Roles { get; set; }
        public IReadOnlyCollection<string>? Permissions { get; set; }
    }

    #endregion
}





using Application.Authorization.Interfaces.Inbound;
using Application.Authorization.Models;
using Application.Authorization.Services;
using Infrastructure.Identity.Models;
using Application.UnitTests.Authorization.Fakes;
using Domain.Authorization.Constants;
using Infrastructure.Identity.Services;
using System.Security.Claims;
using TokenClaimTypes = Domain.Identity.Constants.TokenClaimTypes;

namespace Application.UnitTests.Authorization;

/// <summary>
/// Tests for the new scope directive system using generated PermissionIds constants
/// and IPermissionService from the Application layer.
/// Format: "allow;path" or "allow;path;key=value" or "deny;path"
/// </summary>
public sealed class PermissionServiceScopeDirectiveTests
{
    #region Scope Directive Format Tests (PermissionIds Generated API)

    [Fact]
    public void Allow_WithNoParameters_ReturnsCorrectFormat()
    {
        // Using generated PermissionIds root Read scope
        var directive = PermissionIds.Read.Allow();

        Assert.Equal("allow;_read", directive);
    }

    [Fact]
    public void Deny_WithNoParameters_ReturnsCorrectFormat()
    {
        var directive = PermissionIds.Write.Deny();

        Assert.Equal("deny;_write", directive);
    }

    [Fact]
    public void Allow_WithSingleParameter_ReturnsCorrectFormat()
    {
        // Using generated PermissionIds with userId parameter
        var directive = PermissionIds.Api.Iam.Users.Read.WithUserId("abc123").Allow();

        Assert.Equal("allow;api:iam:users:_read;userId=abc123", directive);
    }

    [Fact]
    public void Allow_WithMultipleParameters_ReturnsCorrectFormat()
    {
        // Using generated PermissionIds with tenantId parameter
        var directive = PermissionIds.SecOpsDebug.V1.Forensics.Read.WithTenantId("tenant-1").Allow();

        Assert.Equal("allow;sec_ops_debug:v1:forensics:_read;tenantId=tenant-1", directive);
    }

    [Fact]
    public void Deny_WithParameters_ReturnsCorrectFormat()
    {
        var directive = PermissionIds.Api.Iam.Users.ResetPassword.WithUserId("abc123").Deny();

        Assert.Equal("deny;api:iam:users:reset_password;userId=abc123", directive);
    }

    [Fact]
    public void ScopeBuilder_Immutability_OriginalUnchanged()
    {
        var builder1 = PermissionIds.Api.Iam.Users.Read.WithUserId("user-1");
        var builder2 = builder1.WithUserId("user-2"); // Chaining should create new instance

        var directive1 = builder1.Allow();
        var directive2 = builder2.Allow();

        // Verify both produce valid distinct directives
        Assert.Contains("userId=user-1", directive1);
        Assert.Contains("userId=user-2", directive2);
        Assert.NotEqual(directive1, directive2);
    }

    [Fact]
    public void LeafPermission_Allow_ReturnsCorrectFormat()
    {
        // Test a leaf permission (not a scope)
        var directive = PermissionIds.Api.Iam.Users.ReadPermission.Allow();

        Assert.Equal("allow;api:iam:users:read", directive);
    }

    [Fact]
    public void LeafPermission_Deny_ReturnsCorrectFormat()
    {
        var directive = PermissionIds.Api.Iam.Users.Update.Deny();

        Assert.Equal("deny;api:iam:users:update", directive);
    }

    #endregion

    #region Permission Checking Tests (Using IPermissionService.HasPermission)

    [Fact]
    public void DirectiveExtraction_ExtractsScopeCorrectly()
    {
        // This test verifies the claims are being set up correctly
        var (service, principal) = CreateServiceWithPrincipal(
            "user-abc",
            PermissionIds.Api.Iam.Users.Read.WithUserId("user-abc").Allow()
        );

        var scopeClaims = principal.Claims
            .Where(c => c.Type == "scope")
            .Select(c => c.Value)
            .ToList();

        // The directive should be in the claims
        Assert.Contains("allow;api:iam:users:_read;userId=user-abc", scopeClaims);
    }

    [Fact]
    public async Task DiagnosticTest_PermissionServiceWithScopeDirective()
    {
        // Create principal with rbac_version=2 and a scope directive
        var (service, principal) = CreateServiceWithPrincipal(
            "user-test",
            "allow;api:iam:users:_read;userId=user-test"
        );

        // First verify the rbac_version is set
        var rbacVersion = principal.Claims.FirstOrDefault(c => c.Type == "rbac_version")?.Value;
        Assert.Equal("2", rbacVersion);

        // Verify the scope claim is set
        var scopeClaims = principal.Claims.Where(c => c.Type == "scope").Select(c => c.Value).ToList();
        Assert.Single(scopeClaims);
        Assert.Equal("allow;api:iam:users:_read;userId=user-test", scopeClaims[0]);

        // Test without userId parameter first - this should NOT pass if scope has userId
        var canReadUsersWithoutUserId = await service.HasPermissionAsync(principal, "api:iam:users:read", CancellationToken.None);
        Assert.False(canReadUsersWithoutUserId, "Should deny without userId when scope has userId parameter");

        // Now test WITH userId parameter - using strongly-typed Permission API
        var canReadUsersWithUserId = await service.HasPermissionAsync(principal, PermissionIds.Api.Iam.Users.ReadPermission.Permission.WithUserId("user-test"), CancellationToken.None);
        Assert.True(canReadUsersWithUserId, "Should allow with matching userId parameter");
    }

    [Fact]
    public async Task DiagnosticTest_PermissionServiceWithBroadScope()
    {
        // Create principal with rbac_version=2 and a BROAD scope (no userId parameter)
        var (service, principal) = CreateServiceWithPrincipal(
            "service-acct",
            "allow;api:iam:users:_read"  // No parameters
        );

        // This should grant access to any api:iam:users:read request
        var canReadUsers = await service.HasPermissionAsync(principal, "api:iam:users:read", CancellationToken.None);
        Assert.True(canReadUsers, "Broad scope should grant access without parameters");

        var canReadUsersWithUserId = await service.HasPermissionAsync(principal, PermissionIds.Api.Iam.Users.ReadPermission.Permission.WithUserId("any-user"), CancellationToken.None);
        Assert.True(canReadUsersWithUserId, "Broad scope should grant access even with userId in request");
    }

    [Fact]
    public async Task HasPermission_UserWithOwnScope_CanAccessOwnData()
    {
        var (service, principal) = CreateServiceWithPrincipal(
            "user-abc",
            PermissionIds.Api.Iam.Users.Read.WithUserId("user-abc").Allow()
        );

        // Check permission for api:iam:users:read with matching userId
        // Using strongly-typed Permission API for type safety
        var hasPermission = await service.HasPermissionAsync(principal, PermissionIds.Api.Iam.Users.ReadPermission.Permission.WithUserId("user-abc"), CancellationToken.None);

        Assert.True(hasPermission);
    }

    [Fact]
    public async Task HasPermission_UserWithDeny_CannotAccessDeniedResource()
    {
        var (service, principal) = CreateServiceWithPrincipal(
            "user-xyz",
            PermissionIds.Api.Iam.Users.Read.Allow(),   // Allow all user reads
            PermissionIds.Api.Iam.Users.Update.Deny()   // But deny updates
        );

        var canRead = await service.HasPermissionAsync(principal, "api:iam:users:read", CancellationToken.None);
        var canUpdate = await service.HasPermissionAsync(principal, "api:iam:users:update", CancellationToken.None);

        Assert.True(canRead);
        Assert.False(canUpdate);
    }

    [Fact]
    public async Task HasPermission_ScopeGrantsLeafPermissions()
    {
        var (service, principal) = CreateServiceWithPrincipal(
            "user-test",
            PermissionIds.Api.Iam.Users.Read.WithUserId("user-test").Allow() // _read scope
        );

        // The _read scope should grant the "read" leaf permission under users
        // Using strongly-typed Permission API
        var hasReadLeaf = await service.HasPermissionAsync(principal, PermissionIds.Api.Iam.Users.ReadPermission.Permission.WithUserId("user-test"), CancellationToken.None);

        Assert.True(hasReadLeaf);
    }

    [Fact]
    public async Task HasAnyPermission_WithMultipleScopes_MatchesAny()
    {
        var (service, principal) = CreateServiceWithPrincipal(
            "user-1",
            PermissionIds.Api.Iam.Users.Read.WithUserId("user-1").Allow()
        );

        var hasAny = await service.HasAnyPermissionAsync(principal, new string[]
        {
            PermissionIds.Api.Iam.Roles.List.Permission, // Not granted
            PermissionIds.Api.Iam.Users.ReadPermission.Permission.WithUserId("user-1")  // Granted
        }, CancellationToken.None);

        Assert.True(hasAny);
    }

    [Fact]
    public async Task HasAllPermissions_RequiresAllMatches()
    {
        var (service, principal) = CreateServiceWithPrincipal(
            "user-2",
            PermissionIds.Api.Iam.Users.Read.WithUserId("user-2").Allow(),
            PermissionIds.Api.Iam.Users.Write.WithUserId("user-2").Allow()
        );

        var hasAll = await service.HasAllPermissionsAsync(principal, new string[]
        {
            PermissionIds.Api.Iam.Users.ReadPermission.Permission.WithUserId("user-2"),
            PermissionIds.Api.Iam.Users.Update.Permission.WithUserId("user-2")
        }, CancellationToken.None);

        Assert.True(hasAll);
    }

    [Fact]
    public async Task HasAllPermissions_FailsWhenOneIsMissing()
    {
        var (service, principal) = CreateServiceWithPrincipal(
            "user-3",
            PermissionIds.Api.Iam.Users.Read.WithUserId("user-3").Allow()
            // Write scope not granted
        );

        var hasAll = await service.HasAllPermissionsAsync(principal, new string[]
        {
            PermissionIds.Api.Iam.Users.ReadPermission.Permission.WithUserId("user-3"),
            PermissionIds.Api.Iam.Users.Update.Permission.WithUserId("user-3") // Not granted
        }, CancellationToken.None);

        Assert.False(hasAll);
    }

    #endregion

    #region Fine-Grained Access Tests

    [Fact]
    public async Task MultiAccountAccess_OnlyAllowsSpecificTenants()
    {
        var (service, principal) = CreateServiceWithPrincipal(
            "admin-user",
            PermissionIds.SecOpsDebug.V1.Forensics.Read.WithTenantId("tenant-1").Allow(),
            PermissionIds.SecOpsDebug.V1.Forensics.Read.WithTenantId("tenant-2").Allow()
        );

        // Using strongly-typed Permission API with tenantId - use List leaf permission
        var canAccessTenant1 = await service.HasPermissionAsync(principal, PermissionIds.SecOpsDebug.V1.Forensics.List.Permission.WithTenantId("tenant-1"), CancellationToken.None);
        var canAccessTenant2 = await service.HasPermissionAsync(principal, PermissionIds.SecOpsDebug.V1.Forensics.List.Permission.WithTenantId("tenant-2"), CancellationToken.None);
        var canAccessTenant3 = await service.HasPermissionAsync(principal, PermissionIds.SecOpsDebug.V1.Forensics.List.Permission.WithTenantId("tenant-3"), CancellationToken.None);

        Assert.True(canAccessTenant1);
        Assert.True(canAccessTenant2);
        Assert.False(canAccessTenant3); // Not granted
    }

    [Fact]
    public async Task ServiceAccount_HasNoUserIdScope_AccessesAll()
    {
        var (service, principal) = CreateServiceWithPrincipal(
            "service-acct",
            PermissionIds.Api.Iam.Users.Read.Allow() // No userId parameter = access all users
        );

        var canAccessUser1 = await service.HasPermissionAsync(principal, PermissionIds.Api.Iam.Users.ReadPermission.Permission.WithUserId("any-user-1"), CancellationToken.None);
        var canAccessUser2 = await service.HasPermissionAsync(principal, PermissionIds.Api.Iam.Users.ReadPermission.Permission.WithUserId("any-user-2"), CancellationToken.None);
        var canAccessNoUser = await service.HasPermissionAsync(principal, PermissionIds.Api.Iam.Users.ReadPermission.Permission, CancellationToken.None);

        Assert.True(canAccessUser1);
        Assert.True(canAccessUser2);
        Assert.True(canAccessNoUser);
    }

    [Fact]
    public async Task RootReadScope_GrantsAllReadLeafPermissions()
    {
        var (service, principal) = CreateServiceWithPrincipal(
            "admin-read",
            PermissionIds.Read.Allow() // Global _read scope
        );

        // Should grant access to any read permission
        var canReadUsers = await service.HasPermissionAsync(principal, "api:iam:users:read", CancellationToken.None);
        var canReadRoles = await service.HasPermissionAsync(principal, "api:iam:roles:list", CancellationToken.None);
        var canReadForensics = await service.HasPermissionAsync(principal, "sec_ops_debug:v1:forensics:list", CancellationToken.None);

        Assert.True(canReadUsers);
        Assert.True(canReadRoles);
        Assert.True(canReadForensics);

        // But NOT write permissions
        var canUpdateUsers = await service.HasPermissionAsync(principal, "api:iam:users:update", CancellationToken.None);
        Assert.False(canUpdateUsers);
    }

    [Fact]
    public async Task NestedResourceAccess_RequiresMatchingParameters()
    {
        var (service, principal) = CreateServiceWithPrincipal(
            "user-nested",
            PermissionIds.SecOpsDebug.V1.Forensics.Read.WithTenantId("tenant-nested").Allow()
        );

        // Request with tenantId parameter - should match (use List leaf permission)
        var withTenantId = await service.HasPermissionAsync(principal, PermissionIds.SecOpsDebug.V1.Forensics.List.Permission.WithTenantId("tenant-nested"), CancellationToken.None);
        Assert.True(withTenantId);

        // Request with different tenantId - should not match
        var differentTenantId = await service.HasPermissionAsync(principal, PermissionIds.SecOpsDebug.V1.Forensics.List.Permission.WithTenantId("other-tenant"), CancellationToken.None);
        Assert.False(differentTenantId);

        // Request with no params - scope has params, so should not match
        var noParams = await service.HasPermissionAsync(principal, PermissionIds.SecOpsDebug.V1.Forensics.List.Permission, CancellationToken.None);
        Assert.False(noParams);
    }

    #endregion

    #region Empty and Boundary Condition Tests

    [Fact]
    public async Task EmptyScope_DeniesAllPermissions()
    {
        var (service, principal) = CreateServiceWithPrincipal(
            "user-empty"
            // No scopes granted
        );

        var hasAnyPermission = await service.HasPermissionAsync(principal, "api:user:profile:read", CancellationToken.None);

        Assert.False(hasAnyPermission);
    }

    [Fact]
    public async Task SingleDenyOnly_AllowsEverythingExceptDenied()
    {
        var (service, principal) = CreateServiceWithPrincipal(
            "user-deny-only",
            PermissionIds.Api.Iam.Users.ResetPassword.Deny() // Only deny, no explicit allows
        );

        // With only deny directives and no allow directives, everything except denied should be allowed
        var canResetPassword = await service.HasPermissionAsync(principal, "api:iam:users:reset_password", CancellationToken.None);
        var canReadUsers = await service.HasPermissionAsync(principal, "api:iam:users:read", CancellationToken.None);

        Assert.False(canResetPassword); // Explicitly denied
        Assert.True(canReadUsers);     // Not denied, so allowed
    }

    [Fact]
    public async Task ConflictingAllowAndDeny_DenyWins()
    {
        var (service, principal) = CreateServiceWithPrincipal(
            "user-conflict",
            PermissionIds.Api.Iam.Users.Read.Allow(),  // Allow user reads
            PermissionIds.Api.Iam.Users.Read.Deny()    // Also deny user reads
        );

        var canRead = await service.HasPermissionAsync(principal, "api:iam:users:read", CancellationToken.None);

        Assert.False(canRead); // Deny should take precedence
    }

    [Fact]
    public async Task GlobalAllowThenSpecificDeny_DenyBlocksSpecific()
    {
        var (service, principal) = CreateServiceWithPrincipal(
            "user-global-deny",
            PermissionIds.Read.Allow(),                           // Allow all reads
            PermissionIds.SecOpsDebug.V1.Forensics.Read.Deny()   // But deny forensics read
        );

        var canReadUsers = await service.HasPermissionAsync(principal, "api:iam:users:read", CancellationToken.None);
        var canReadForensics = await service.HasPermissionAsync(principal, "sec_ops_debug:v1:forensics:list", CancellationToken.None);

        Assert.True(canReadUsers);    // Allowed by global _read
        Assert.False(canReadForensics); // Explicitly denied
    }

    #endregion

    #region Privilege Escalation Tests

    [Fact]
    public async Task PrivilegeEscalation_UserCannotAccessOtherUsersData()
    {
        var (service, principal) = CreateServiceWithPrincipal(
            "user-own",
            PermissionIds.Api.Iam.Users.Read.WithUserId("user-own").Allow()
        );

        // Using strongly-typed Permission API
        var canAccessOwn = await service.HasPermissionAsync(principal, PermissionIds.Api.Iam.Users.ReadPermission.Permission.WithUserId("user-own"), CancellationToken.None);
        var canAccessOther = await service.HasPermissionAsync(principal, PermissionIds.Api.Iam.Users.ReadPermission.Permission.WithUserId("other-user"), CancellationToken.None);

        Assert.True(canAccessOwn);
        Assert.False(canAccessOther); // Cannot access another user's data
    }

    [Fact]
    public async Task PathTraversal_CannotEscapeHierarchy()
    {
        var (service, principal) = CreateServiceWithPrincipal(
            "user-path",
            PermissionIds.Api.Iam.Users.Read.WithUserId("user-path").Allow()
        );

        // Having users:_read should not grant access to sibling roles
        var canReadRoles = await service.HasPermissionAsync(principal, "api:iam:roles:list", CancellationToken.None);

        Assert.False(canReadRoles);
    }

    [Fact]
    public async Task NestedScopeHierarchy_ChildDoesNotGrantParent()
    {
        var (service, principal) = CreateServiceWithPrincipal(
            "user-child",
            PermissionIds.Api.Iam.Users.Read.WithUserId("user-child").Allow()
        );

        // Child scope does not grant parent access
        // Using strongly-typed Permission API - _read scope doesn't have Permission since it's not a leaf
        var hasApiRead = await service.HasPermissionAsync(principal, "api:_read;userId=user-child", CancellationToken.None);

        Assert.False(hasApiRead); // Child doesn't grant parent
    }

    #endregion

    #region Scope Directive Format Enforcement Tests

    [Fact]
    public async Task PlainScopeString_WithoutDirectivePrefix_IsIgnored()
    {
        // Scope claims must use the directive format "allow;path" or "deny;path".
        // A plain permission path like "api:iam:users:_read" cannot be parsed as a
        // ScopeDirective and is silently skipped, resulting in an empty scope list.
        var claims = new[]
        {
            new Claim(TokenClaimTypes.Subject, "legacy-user"),
            new Claim(TokenClaimTypes.Name, "legacy@example.com"),
            new Claim("scope", "api:iam:users:_read")
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        var service = CreatePermissionService();

        var hasPermission = await service.HasPermissionAsync(principal, "api:iam:users:read", CancellationToken.None);

        Assert.False(hasPermission);
    }

    [Fact]
    public async Task PlainScopeString_WithRbacVersionClaim_IsStillIgnored()
    {
        // Even with rbac_version present, a scope claim that lacks the directive
        // prefix ("allow;" / "deny;") is unparseable and effectively absent.
        var claims = new[]
        {
            new Claim(TokenClaimTypes.Subject, "v1-user"),
            new Claim(TokenClaimTypes.Name, "v1@example.com"),
            new Claim("rbac_version", "1"),
            new Claim("scope", "api:iam:users:_read")
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        var service = CreatePermissionService();

        var hasPermission = await service.HasPermissionAsync(principal, "api:iam:users:read", CancellationToken.None);

        Assert.False(hasPermission);
    }

    [Fact]
    public async Task DirectiveFormatScope_GrantsOnlyExplicitPermissions()
    {
        // A properly formatted directive scope grants only the specified permission.
        // Unrelated permissions remain denied.
        var claims = new[]
        {
            new Claim(TokenClaimTypes.Subject, "v2-user"),
            new Claim(TokenClaimTypes.Name, "v2@example.com"),
            new Claim("rbac_version", "2"),
            new Claim("scope", PermissionIds.Api.Iam.Users.Read.WithUserId("v2-user").Allow())
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        var service = CreatePermissionService();

        var hasGranted = await service.HasPermissionAsync(principal, PermissionIds.Api.Iam.Users.ReadPermission.Permission.WithUserId("v2-user"), CancellationToken.None);
        var hasNotGranted = await service.HasPermissionAsync(principal, PermissionIds.Api.Iam.Roles.List.Permission, CancellationToken.None);

        Assert.True(hasGranted);
        Assert.False(hasNotGranted);
    }

    #endregion

    #region Input Validation Tests

    [Fact]
    public async Task HasPermission_NullPrincipal_ThrowsArgumentNullException()
    {
        var service = CreatePermissionService();

        await Assert.ThrowsAsync<ArgumentNullException>(() => service.HasPermissionAsync(null!, "api:user:profile:read", CancellationToken.None));
    }

    [Fact]
    public async Task HasPermission_EmptyPermissionPath_ReturnsFalse()
    {
        var (service, principal) = CreateServiceWithPrincipal(
            "user-test",
            PermissionIds.Read.Allow()
        );

        var result = await service.HasPermissionAsync(principal, "", CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task HasPermission_WhitespacePermissionPath_ReturnsFalse()
    {
        var (service, principal) = CreateServiceWithPrincipal(
            "user-test",
            PermissionIds.Read.Allow()
        );

        var result = await service.HasPermissionAsync(principal, "   ", CancellationToken.None);

        Assert.False(result);
    }

    #endregion

    #region Generated PermissionIds API Tests

    [Fact]
    public void PermissionIds_All_ContainsExpectedPaths()
    {
        // Verify the generated PermissionIds.All collection exists and has content
        Assert.NotEmpty(PermissionIds.All);
        Assert.Contains("api:iam:users:read", PermissionIds.All);
        Assert.Contains("api:iam:users:update", PermissionIds.All);
        Assert.Contains("sec_ops_debug:v1:forensics:list", PermissionIds.All);
    }

    [Fact]
    public void PermissionIds_AllParameters_ContainsExpectedParameters()
    {
        // Verify the generated AllParameters collection
        Assert.NotEmpty(PermissionIds.AllParameters);
        Assert.Contains("userId", PermissionIds.AllParameters);
    }

    [Fact]
    public void PermissionMetadata_RLeafPermissions_ContainsReadLeafs()
    {
        // Read leaf permissions should include read-category leaf nodes
        Assert.NotEmpty(PermissionMetadata.RLeafPermissions);
        Assert.Contains("api:iam:users:read", PermissionMetadata.RLeafPermissions);
    }

    [Fact]
    public void PermissionMetadata_WLeafPermissions_ContainsWriteLeafs()
    {
        // Write leaf permissions should include write-category leaf nodes
        Assert.NotEmpty(PermissionMetadata.WLeafPermissions);
        Assert.Contains("api:iam:users:update", PermissionMetadata.WLeafPermissions);
    }

    #endregion

    #region Inherited Parameter Tests (regression guard)

    // These tests verify that parameters declared on an ancestor Node(...) remain
    // reachable via With{Param} on descendant leaves whose own parameter list is empty.
    // The downstream-fork bug: leaves with no local parameters silently dropped the
    // inherited parameter chain and emitted bare paths. This guards against the
    // equivalent class of bug in the suffix-directive emitter.

    [Fact]
    public void InheritedParameter_OnLeaf_AllowProducesParameterizedDirective()
    {
        // `me` is a parameterless RLeaf under `auth` (which declares userId).
        var directive = PermissionIds.Api.Auth.Me.WithUserId("u123").Allow();

        Assert.Equal("allow;api:auth:me;userId=u123", directive);
    }

    [Fact]
    public void InheritedParameter_OnLeaf_DenyProducesParameterizedDirective()
    {
        var directive = PermissionIds.Api.Auth.Me.WithUserId("u123").Deny();

        Assert.Equal("deny;api:auth:me;userId=u123", directive);
    }

    [Fact]
    public void InheritedParameter_OnLeaf_IdentifierProducesParameterizedPath()
    {
        var identifier = PermissionIds.Api.Auth.Me.WithUserId("u123").Identifier();

        Assert.Equal("api:auth:me;userId=u123", identifier);
    }

    [Fact]
    public void InheritedParameter_OnLeafPermissionRequestBuilder_ToStringProducesParameterizedPath()
    {
        var perm = PermissionIds.Api.Auth.Me.Permission.WithUserId("u123").ToString();

        Assert.Equal("api:auth:me;userId=u123", perm);
    }

    [Fact]
    public void InheritedParameter_OnLeafPermissionRequestBuilder_ImplicitStringConversion()
    {
        // Implicit string conversion must also include the parameter suffix.
        string perm = PermissionIds.Api.Auth.Me.Permission.WithUserId("u123");

        Assert.Equal("api:auth:me;userId=u123", perm);
    }

#if DEBUG
    [Fact]
    public void InheritedParameter_MultiLevelAncestor_LeafExposesAllAncestorParameters()
    {
        // `timeline` is a parameterless RLeaf under `incident_workspace`
        // (declares incidentId, region) under `sec_ops_debug` (declares tenantId).
        // All three ancestor parameters must be reachable as static With{Param} factories
        // on the leaf, and as instance methods on the returned ScopeBuilder.
        var directive = PermissionIds.SecOpsDebug.V1.IncidentWorkspace.Timeline
            .WithIncidentId("inc-42")
            .WithRegion("us-east-1")
            .Allow();

        Assert.Equal("allow;sec_ops_debug:v1:incident_workspace:timeline;incidentId=inc-42;region=us-east-1", directive);
    }

    [Fact]
    public void InheritedParameter_MultiLevelAncestor_AllThreeParametersChainable()
    {
        var directive = PermissionIds.SecOpsDebug.V1.IncidentWorkspace.Timeline
            .WithTenantId("tenant-a")
            .WithIncidentId("inc-42")
            .WithRegion("us-east-1")
            .Allow();

        Assert.Equal("allow;sec_ops_debug:v1:incident_workspace:timeline;tenantId=tenant-a;incidentId=inc-42;region=us-east-1", directive);
    }

    [Fact]
    public void InheritedParameter_MultiLevelAncestor_PermissionRequestBuilderChain()
    {
        var perm = PermissionIds.SecOpsDebug.V1.IncidentWorkspace.Timeline.Permission
            .WithIncidentId("inc-42")
            .WithRegion("us-east-1")
            .ToString();

        Assert.Equal("sec_ops_debug:v1:incident_workspace:timeline;incidentId=inc-42;region=us-east-1", perm);
    }

    [Fact]
    public void InheritedParameter_MultiLevelAncestor_DenyDirectiveIncludesAllParameters()
    {
        var directive = PermissionIds.SecOpsDebug.V1.IncidentWorkspace.Timeline
            .WithIncidentId("inc-42")
            .WithRegion("us-east-1")
            .Deny();

        Assert.Equal("deny;sec_ops_debug:v1:incident_workspace:timeline;incidentId=inc-42;region=us-east-1", directive);
    }

    [Fact]
    public void InheritedParameter_OnScope_ChainStartsFromAnyInheritedParameter()
    {
        // _read scope under incident_workspace inherits tenantId, incidentId, region.
        // Verify any of them can start the chain (i.e., all are emitted as static factories).
        var byIncidentId = PermissionIds.SecOpsDebug.V1.IncidentWorkspace.Read
            .WithIncidentId("inc-9")
            .WithRegion("eu-west-1")
            .Allow();

        Assert.Equal("allow;sec_ops_debug:v1:incident_workspace:_read;incidentId=inc-9;region=eu-west-1", byIncidentId);

        var byRegion = PermissionIds.SecOpsDebug.V1.IncidentWorkspace.Read
            .WithRegion("eu-west-1")
            .WithIncidentId("inc-9")
            .Allow();

        Assert.Equal("allow;sec_ops_debug:v1:incident_workspace:_read;region=eu-west-1;incidentId=inc-9", byRegion);
    }
#endif

    #endregion

    #region Helper Methods

    private static IPermissionService CreatePermissionService(JwtConfiguration? jwtConfig = null)
    {
        jwtConfig ??= new JwtConfiguration
        {
            Secret = "super-secret-key-value-which-is-long-enough-for-tests",
            Issuer = "https://test-issuer.example.com",
            Audience = "https://test-audience.example.com",
            DefaultExpiration = TimeSpan.FromHours(1),
            ClockSkew = TimeSpan.FromMinutes(5)
        };

        var lazyJwtFactory = new Lazy<Func<CancellationToken, Task<JwtConfiguration>>>(() => _ => Task.FromResult(jwtConfig));
        var jwtTokenService = new JwtTokenService(lazyJwtFactory);

        var tokenProvider = new TokenProvider(jwtTokenService);
        return new PermissionService(tokenProvider, new InMemoryRoleRepository());
    }

    /// <summary>
    /// Creates a ClaimsPrincipal directly with scope claims in the new directive format.
    /// This bypasses token generation validation to test HasPermission behavior.
    /// </summary>
    private static (IPermissionService Service, ClaimsPrincipal Principal) CreateServiceWithPrincipal(
        string userId,
        params string[] scopes)
    {
        var service = CreatePermissionService();

        var claimsList = new List<Claim>
        {
            new Claim(TokenClaimTypes.Subject, userId),
            new Claim(TokenClaimTypes.Name, $"{userId}@example.com"),
            new Claim("rbac_version", "2") // Use new RBAC version to enable directive-based evaluation
        };

        foreach (var scope in scopes)
        {
            claimsList.Add(new Claim("scope", scope));
        }

        var identity = new ClaimsIdentity(claimsList, "Test");
        var principal = new ClaimsPrincipal(identity);

        return (service, principal);
    }

    #endregion
}


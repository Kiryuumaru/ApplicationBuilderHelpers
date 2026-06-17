using Domain.Authorization.Constants;
using Domain.Authorization.Enums;
using Domain.Authorization.Models;
using Domain.Authorization.Utilities;
using Domain.Authorization.ValueObjects;

namespace Domain.UnitTests.Authorization;

/// <summary>
/// Tests for ScopeEvaluator to verify new directive-based scope evaluation works correctly.
/// </summary>
public sealed class ScopeEvaluatorTests
{
    [Fact]
    public void HasPermission_GlobalReadScope_GrantsRLeafPermission()
    {
        var scope = new[]
        {
            ScopeDirective.Allow("_read")
        };

        var result = ScopeEvaluator.HasPermission(scope, "api:iam:users:read");

        Assert.True(result);
    }

    [Fact]
    public void HasPermission_GlobalReadScope_DoesNotGrantWLeafPermission()
    {
        var scope = new[]
        {
            ScopeDirective.Allow("_read")
        };

        var result = ScopeEvaluator.HasPermission(scope, "api:iam:users:update");

        Assert.False(result);
    }

    [Fact]
    public void HasPermission_ScopedReadScope_GrantsRLeafUnderParent()
    {
        var scope = new[]
        {
            ScopeDirective.Allow("api:iam:users:_read")
        };

        var result = ScopeEvaluator.HasPermission(scope, "api:iam:users:read");

        Assert.True(result);
    }

    [Fact]
    public void HasPermission_ScopedReadScope_DoesNotGrantSibling()
    {
        var scope = new[]
        {
            ScopeDirective.Allow("api:iam:users:_read")
        };

        var result = ScopeEvaluator.HasPermission(scope, "api:iam:roles:read");

        Assert.False(result);
    }

    [Fact]
    public void HasPermission_WithMatchingParameters_Grants()
    {
        var scope = new[]
        {
            ScopeDirective.Allow("api:iam:users:_read", ("userId", "user-123"))
        };
        var requestParams = new Dictionary<string, string> { ["userId"] = "user-123" };

        var result = ScopeEvaluator.HasPermission(scope, "api:iam:users:read", requestParams);

        Assert.True(result);
    }

    [Fact]
    public void HasPermission_WithNonMatchingParameters_Denies()
    {
        var scope = new[]
        {
            ScopeDirective.Allow("api:iam:users:_read", ("userId", "user-123"))
        };
        var requestParams = new Dictionary<string, string> { ["userId"] = "other-user" };

        var result = ScopeEvaluator.HasPermission(scope, "api:iam:users:read", requestParams);

        Assert.False(result);
    }

    [Fact]
    public void HasPermission_ScopeWithParamsButNoRequestParams_Denies()
    {
        var scope = new[]
        {
            ScopeDirective.Allow("api:iam:users:_read", ("userId", "user-123"))
        };

        // Request has no parameters
        var result = ScopeEvaluator.HasPermission(scope, "api:iam:users:read", null);

        Assert.False(result);
    }

    [Fact]
    public void HasPermission_ScopeWithoutParamsButRequestHasParams_Grants()
    {
        // Broad scope - no userId restriction
        var scope = new[]
        {
            ScopeDirective.Allow("api:iam:users:_read")
        };
        var requestParams = new Dictionary<string, string> { ["userId"] = "any-user" };

        var result = ScopeEvaluator.HasPermission(scope, "api:iam:users:read", requestParams);

        Assert.True(result);
    }

    [Fact]
    public void HasPermission_DenyTakesPrecedence()
    {
        var scope = new[]
        {
            ScopeDirective.Allow("_read"),
            ScopeDirective.Deny("api:iam:roles:read")
        };

        var canReadUsers = ScopeEvaluator.HasPermission(scope, "api:iam:users:read");
        var canReadRoles = ScopeEvaluator.HasPermission(scope, "api:iam:roles:read");

        Assert.True(canReadUsers);
        Assert.False(canReadRoles);
    }

    [Fact]
    public void HasPermission_EmptyScope_DeniesAll()
    {
        var scope = Array.Empty<ScopeDirective>();

        var result = ScopeEvaluator.HasPermission(scope, "api:iam:users:read");

        Assert.False(result);
    }

    [Fact]
    public void HasPermission_DenyOnlyMode_AllowsEverythingExceptDenied()
    {
        var scope = new[]
        {
            ScopeDirective.Deny("api:iam:users:reset_password")
        };

        var canResetPassword = ScopeEvaluator.HasPermission(scope, "api:iam:users:reset_password");
        var canReadUsers = ScopeEvaluator.HasPermission(scope, "api:iam:users:read");

        Assert.False(canResetPassword);
        Assert.True(canReadUsers);
    }

    [Fact]
    public void DirectiveParsing_AllowFormat_ParsesCorrectly()
    {
        var directive = ScopeDirective.Parse("allow;api:iam:users:_read;userId=abc123");

        Assert.Equal(ScopeDirectiveType.Allow, directive.Type);
        Assert.Equal("api:iam:users:_read", directive.PermissionPath);
        Assert.Single(directive.Parameters);
        Assert.Equal("abc123", directive.Parameters["userId"]);
    }

    [Fact]
    public void DirectiveParsing_DenyFormat_ParsesCorrectly()
    {
        var directive = ScopeDirective.Parse("deny;api:iam:users:reset_password");

        Assert.Equal(ScopeDirectiveType.Deny, directive.Type);
        Assert.Equal("api:iam:users:reset_password", directive.PermissionPath);
        Assert.Empty(directive.Parameters);
    }

    [Fact]
    public void PermissionIds_GeneratedAllowDirective_ParsesCorrectly()
    {
        // Test the generated directive format
        var directiveString = PermissionIds.Api.Iam.Users.Read.WithUserId("user-test").Allow();
        var directive = ScopeDirective.Parse(directiveString);

        Assert.Equal(ScopeDirectiveType.Allow, directive.Type);
        Assert.Equal("api:iam:users:_read", directive.PermissionPath);
        Assert.Equal("user-test", directive.Parameters["userId"]);
    }

    [Fact]
    public void Integration_ParsedDirective_WorksWithEvaluator()
    {
        var directiveString = PermissionIds.Api.Iam.Users.Read.WithUserId("user-123").Allow();
        var directive = ScopeDirective.Parse(directiveString);
        var scope = new[] { directive };
        var requestParams = new Dictionary<string, string> { ["userId"] = "user-123" };

        var result = ScopeEvaluator.HasPermission(scope, "api:iam:users:read", requestParams);

        Assert.True(result);
    }

    [Fact]
    public void RLeafPermissions_ContainsExpectedPaths()
    {
        var rLeafs = ScopeEvaluator.GetRLeafPermissions();

        Assert.Contains("api:iam:users:read", rLeafs);
        Assert.Contains("api:iam:users:list", rLeafs);
        Assert.Contains("api:iam:roles:list", rLeafs);
    }

    [Fact]
    public void WLeafPermissions_ContainsExpectedPaths()
    {
        var wLeafs = ScopeEvaluator.GetWLeafPermissions();

        Assert.Contains("api:iam:users:update", wLeafs);
        Assert.Contains("api:iam:users:delete", wLeafs);
        Assert.Contains("api:iam:users:reset_password", wLeafs);
    }

    [Fact]
    public void PermissionParameterHierarchy_ContainsInheritedParams()
    {
        // Verify that leaf permissions inherit parameters from ancestors
        var allPermissions = Permissions.GetAll();
        var usersReadPermission = allPermissions.First(p => p.Path == "api:iam:users:read");

        var paramHierarchy = usersReadPermission.GetParameterHierarchy();

        Assert.Contains("userId", paramHierarchy);
    }

    [Fact]
    public void PermissionParsing_ExtractsParametersCorrectly_SemicolonFormat()
    {
        // Test that parsing NEW semicolon format api:iam:users:read;userId=abc works correctly
        var success = Permission.TryParseIdentifier("api:iam:users:read;userId=abc", out var parsed);

        Assert.True(success);
        Assert.Equal("api:iam:users:read", parsed.Canonical);
        Assert.True(parsed.Parameters.ContainsKey("userId"));
        Assert.Equal("abc", parsed.Parameters["userId"]);
    }

    [Fact]
    public void PermissionParsing_SemicolonFormat_WithMultipleParameters()
    {
        // Test that parsing semicolon format with multiple params works
        var success = Permission.TryParseIdentifier("api:iam:users:read;userId=user-1", out var parsed);

        Assert.True(success);
        Assert.Equal("api:iam:users:read", parsed.Canonical);
        Assert.Equal("user-1", parsed.Parameters["userId"]);
    }

    [Fact]
    public void PermissionParsing_StronglyTypedApi_ProducesSemicolonFormat()
    {
        // Test that the strongly-typed Permission API produces correct semicolon format
        string permissionString = PermissionIds.Api.Iam.Users.ReadPermission.Permission.WithUserId("abc");
        
        Assert.Equal("api:iam:users:read;userId=abc", permissionString);
        
        var success = Permission.TryParseIdentifier(permissionString, out var parsed);
        Assert.True(success);
        Assert.Equal("api:iam:users:read", parsed.Canonical);
        Assert.Equal("abc", parsed.Parameters["userId"]);
    }

    [Fact]
    public void ScopeEvaluator_WithParametrizedRequest_MatchesParametrizedScope()
    {
        // Test that ScopeEvaluator correctly matches parametrized requests to parametrized scopes
        var scopes = new[] { ScopeDirective.Allow("api:iam:users:_read", new Dictionary<string, string> { { "userId", "abc" } }) };
        var requestParams = new Dictionary<string, string> { { "userId", "abc" } };

        var hasPermission = ScopeEvaluator.HasPermission(scopes, "api:iam:users:read", requestParams);

        Assert.True(hasPermission);
    }
}


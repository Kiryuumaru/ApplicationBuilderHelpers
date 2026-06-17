using Application.Client.Authorization.Interfaces.Inbound;
using Application.Client.Authorization.Services;
using Application.Client.Identity.Interfaces.Inbound;
using Application.Client.Identity.Models;
using Domain.Authorization.Constants;
using NSubstitute;

namespace Application.UnitTests.Authorization;

/// <summary>
/// Tests for IClientPermissionService that evaluates permissions from AuthState.
/// </summary>
public class ClientPermissionServiceTests
{
    private readonly IAuthStateProvider _authStateProvider;
    private readonly IClientPermissionService _permissionService;

    public ClientPermissionServiceTests()
    {
        _authStateProvider = Substitute.For<IAuthStateProvider>();
        _permissionService = new ClientPermissionService(_authStateProvider);
    }

    private void SetupPermissions(params string[] permissions)
    {
        var authState = new AuthState
        {
            IsAuthenticated = true,
            Permissions = permissions.ToList()
        };
        _authStateProvider.CurrentState.Returns(authState);
    }

    #region HasPermission Tests

    [Fact]
    public void HasPermission_WithAllowDirective_ReturnsTrue()
    {
        SetupPermissions("allow;api:iam:users:list");

        var result = _permissionService.HasPermission("api:iam:users:list");

        Assert.True(result);
    }

    [Fact]
    public void HasPermission_WithDenyDirective_ReturnsFalse()
    {
        SetupPermissions("deny;api:iam:users:list");

        var result = _permissionService.HasPermission("api:iam:users:list");

        Assert.False(result);
    }

    [Fact]
    public void HasPermission_WithNoMatchingDirective_ReturnsFalse()
    {
        SetupPermissions("allow;api:iam:roles:list");

        var result = _permissionService.HasPermission("api:iam:users:list");

        Assert.False(result);
    }

    [Fact]
    public void HasPermission_DenyOverridesAllow()
    {
        SetupPermissions(
            "allow;api:iam:users:_read",
            "deny;api:iam:users:list"
        );

        var canRead = _permissionService.HasPermission("api:iam:users:read");
        var canList = _permissionService.HasPermission("api:iam:users:list");

        Assert.True(canRead);
        Assert.False(canList);
    }

    [Fact]
    public void HasPermission_WithReadScope_GrantsAllReadChildren()
    {
        SetupPermissions("allow;api:iam:users:_read");

        var canList = _permissionService.HasPermission("api:iam:users:list");
        var canRead = _permissionService.HasPermission("api:iam:users:read");
        var canReadPermissions = _permissionService.HasPermission("api:iam:users:permissions");

        Assert.True(canList);
        Assert.True(canRead);
        Assert.True(canReadPermissions);
    }

    [Fact]
    public void HasPermission_WithWriteScope_GrantsAllWriteChildren()
    {
        SetupPermissions("allow;api:iam:users:_write");

        var canUpdate = _permissionService.HasPermission("api:iam:users:update");
        var canDelete = _permissionService.HasPermission("api:iam:users:delete");
        var canResetPassword = _permissionService.HasPermission("api:iam:users:reset_password");

        Assert.True(canUpdate);
        Assert.True(canDelete);
        Assert.True(canResetPassword);
    }

    [Fact]
    public void HasPermission_ReadScope_DoesNotGrantWritePermissions()
    {
        SetupPermissions("allow;api:iam:users:_read");

        var canUpdate = _permissionService.HasPermission("api:iam:users:update");
        var canDelete = _permissionService.HasPermission("api:iam:users:delete");

        Assert.False(canUpdate);
        Assert.False(canDelete);
    }

    [Fact]
    public void HasPermission_EmptyPermissions_ReturnsFalse()
    {
        SetupPermissions();

        var result = _permissionService.HasPermission("api:iam:users:list");

        Assert.False(result);
    }

    [Fact]
    public void HasPermission_NullPermissions_ReturnsFalse()
    {
        var authState = new AuthState
        {
            IsAuthenticated = true,
            Permissions = null!
        };
        _authStateProvider.CurrentState.Returns(authState);

        var result = _permissionService.HasPermission("api:iam:users:list");

        Assert.False(result);
    }

    [Fact]
    public void HasPermission_UsingGeneratedPermissionIds()
    {
        SetupPermissions("allow;api:iam:users:_read");

        var result = _permissionService.HasPermission(PermissionIds.Api.Iam.Users.List.Identifier);

        Assert.True(result);
    }

    #endregion

    #region HasAnyPermission Tests

    [Fact]
    public void HasAnyPermission_OneMatches_ReturnsTrue()
    {
        SetupPermissions("allow;api:iam:users:list");

        var result = _permissionService.HasAnyPermission(
            "api:iam:users:list",
            "api:iam:roles:list"
        );

        Assert.True(result);
    }

    [Fact]
    public void HasAnyPermission_NoneMatch_ReturnsFalse()
    {
        SetupPermissions("allow;api:iam:permissions:list");

        var result = _permissionService.HasAnyPermission(
            "api:iam:users:list",
            "api:iam:roles:list"
        );

        Assert.False(result);
    }

    [Fact]
    public void HasAnyPermission_AllMatch_ReturnsTrue()
    {
        SetupPermissions(
            "allow;api:iam:users:list",
            "allow;api:iam:roles:list"
        );

        var result = _permissionService.HasAnyPermission(
            "api:iam:users:list",
            "api:iam:roles:list"
        );

        Assert.True(result);
    }

    [Fact]
    public void HasAnyPermission_EmptyArray_ReturnsFalse()
    {
        SetupPermissions("allow;api:iam:users:list");

        var result = _permissionService.HasAnyPermission();

        Assert.False(result);
    }

    #endregion

    #region HasAllPermissions Tests

    [Fact]
    public void HasAllPermissions_AllMatch_ReturnsTrue()
    {
        SetupPermissions(
            "allow;api:iam:users:list",
            "allow;api:iam:roles:list"
        );

        var result = _permissionService.HasAllPermissions(
            "api:iam:users:list",
            "api:iam:roles:list"
        );

        Assert.True(result);
    }

    [Fact]
    public void HasAllPermissions_OneMissing_ReturnsFalse()
    {
        SetupPermissions("allow;api:iam:users:list");

        var result = _permissionService.HasAllPermissions(
            "api:iam:users:list",
            "api:iam:roles:list"
        );

        Assert.False(result);
    }

    [Fact]
    public void HasAllPermissions_EmptyArray_ReturnsFalse()
    {
        SetupPermissions("allow;api:iam:users:list");

        var result = _permissionService.HasAllPermissions();

        Assert.False(result);
    }

    [Fact]
    public void HasAllPermissions_WithScopeGrantingAll_ReturnsTrue()
    {
        SetupPermissions("allow;api:iam:users:_read");

        var result = _permissionService.HasAllPermissions(
            "api:iam:users:list",
            "api:iam:users:read",
            "api:iam:users:permissions"
        );

        Assert.True(result);
    }

    #endregion

    #region Malformed Directive Tests (Chaos Tests)

    [Fact]
    public void HasPermission_MalformedDirective_InvalidFormat_GracefullyIgnored()
    {
        SetupPermissions(
            "invalid-format",
            "allow;api:iam:users:list"
        );

        var result = _permissionService.HasPermission("api:iam:users:list");

        Assert.True(result);
    }

    [Fact]
    public void HasPermission_MalformedDirective_EmptyString_GracefullyIgnored()
    {
        SetupPermissions(
            "",
            "allow;api:iam:users:list"
        );

        var result = _permissionService.HasPermission("api:iam:users:list");

        Assert.True(result);
    }

    [Fact]
    public void HasPermission_MalformedDirective_MissingPermission_GracefullyIgnored()
    {
        SetupPermissions(
            "allow;",
            "allow;api:iam:users:list"
        );

        var result = _permissionService.HasPermission("api:iam:users:list");

        Assert.True(result);
    }

    [Fact]
    public void HasPermission_MalformedDirective_UnknownPermission_NoMatch()
    {
        SetupPermissions("allow;nonexistent:permission:path");

        var result = _permissionService.HasPermission("api:iam:users:list");

        Assert.False(result);
    }

    [Fact]
    public void HasPermission_MalformedDirective_SqlInjection_TreatedAsLiteralString()
    {
        SetupPermissions("allow;'; DROP TABLE users;--");

        var result = _permissionService.HasPermission("api:iam:users:list");

        Assert.False(result);
    }

    [Fact]
    public void HasPermission_MalformedDirective_XssAttempt_TreatedAsLiteralString()
    {
        SetupPermissions("allow;<script>alert(1)</script>");

        var result = _permissionService.HasPermission("api:iam:users:list");

        Assert.False(result);
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void HasPermission_WithParameterizedScope_Ignored_WhenNoRequestParams()
    {
        SetupPermissions("allow;api:iam:users:_read;userId=abc");

        var result = _permissionService.HasPermission("api:iam:users:list");

        Assert.False(result);
    }

    [Fact]
    public void HasPermission_GlobalReadScope_GrantsAllReadPermissions()
    {
        SetupPermissions("allow;_read");

        var canReadUsers = _permissionService.HasPermission("api:iam:users:list");
        var canReadRoles = _permissionService.HasPermission("api:iam:roles:list");

        Assert.True(canReadUsers);
        Assert.True(canReadRoles);
    }

    [Fact]
    public void HasPermission_GlobalWriteScope_GrantsAllWritePermissions()
    {
        SetupPermissions("allow;_write");

        var canUpdateUsers = _permissionService.HasPermission("api:iam:users:update");
        var canDeleteRoles = _permissionService.HasPermission("api:iam:roles:delete");

        Assert.True(canUpdateUsers);
        Assert.True(canDeleteRoles);
    }

    [Fact]
    public void HasPermission_DenyAtParent_BlocksAllChildren()
    {
        SetupPermissions(
            "allow;_read",
            "deny;api:iam:users:_read"
        );

        var canReadRoles = _permissionService.HasPermission("api:iam:roles:list");
        var canReadUsers = _permissionService.HasPermission("api:iam:users:list");

        Assert.True(canReadRoles);
        Assert.False(canReadUsers);
    }

    [Fact]
    public void HasPermission_AllowAtParent_DenyAtSpecificChild()
    {
        SetupPermissions(
            "allow;api:iam:users:_read",
            "deny;api:iam:users:list"
        );

        var canRead = _permissionService.HasPermission("api:iam:users:read");
        var canList = _permissionService.HasPermission("api:iam:users:list");

        Assert.True(canRead);
        Assert.False(canList);
    }

    [Fact]
    public void HasPermission_User1000Permissions_EvaluatesCorrectly()
    {
        var permissions = new List<string>();
        for (int i = 0; i < 999; i++)
        {
            permissions.Add($"allow;dummy:permission:{i}");
        }
        permissions.Add("allow;api:iam:users:list");
        SetupPermissions(permissions.ToArray());

        var result = _permissionService.HasPermission("api:iam:users:list");

        Assert.True(result);
    }

    #endregion
}

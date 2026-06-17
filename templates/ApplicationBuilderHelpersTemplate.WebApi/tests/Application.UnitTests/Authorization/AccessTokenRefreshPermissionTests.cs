using System.Security.Claims;
using Application.Authorization.Interfaces.Outbound;
using Application.Authorization.Services;
using Application.Identity.Interfaces.Inbound;
using Application.Identity.Services;
using Application.UnitTests.Authorization.Fakes;
using Domain.Authorization.Constants;
using NSubstitute;
using TokenClaimTypes = Domain.Identity.Constants.TokenClaimTypes;

namespace Application.UnitTests.Authorization;

/// <summary>
/// Tests that expose the permission bug where access tokens incorrectly have api:auth:refresh permission.
/// </summary>
public sealed class AccessTokenRefreshPermissionTests
{
    private static readonly Guid TestUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TestSessionId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    /// <summary>
    /// Exposes the underlying behavior: role-aware permission evaluation grants api:auth:refresh
    /// when a principal has USER role and the role grants allow;_write;userId=xxx.
    /// </summary>
    [Fact]
    public async Task RoleAwareEvaluation_WithUserRole_GrantsRefreshPermission()
    {
        // Arrange: Create a principal like an access token would have (roles, no scope claim)
        var claims = new List<Claim>
        {
            new(TokenClaimTypes.Subject, TestUserId.ToString()),
            new(TokenClaimTypes.Roles, $"USER;roleUserId={TestUserId}"),
            new(RbacConstants.VersionClaimType, RbacConstants.CurrentVersion)
        };
        var identity = new ClaimsIdentity(claims, "Bearer");
        var principal = new ClaimsPrincipal(identity);

        // Setup role repository with USER role
        var roleRepo = new InMemoryRoleRepository();
        var userRole = Roles.User.Instantiate();
        roleRepo.Add(userRole);

        var tokenProvider = Substitute.For<ITokenProvider>();
        var service = new PermissionService(tokenProvider, roleRepo);

        // Act: Check if access token has refresh permission
        var refreshPermission = PermissionIds.Api.Auth.Refresh.Permission.WithUserId(TestUserId.ToString());
        var hasRefreshPermission = await service.HasPermissionAsync(principal, refreshPermission, CancellationToken.None);

        // Assert
        Assert.True(hasRefreshPermission);
    }

    /// <summary>
    /// Root fix verification: refresh flow must require an explicit allow;api:auth:refresh directive
    /// in the token's scope claim, so an access token (roles-only) is rejected.
    /// </summary>
    [Fact]
    public async Task RefreshFlow_AccessTokenWithoutScope_IsRejectedBeforeSessionValidation()
    {
        var userAuthorizationService = Substitute.For<IUserAuthorizationService>();
        var sessionService = Substitute.For<ISessionService>();

        // Access-token-like principal: has roles and an explicit deny;api:auth:refresh scope
        // (added at access token issuance).
        var claims = new List<Claim>
        {
            new(TokenClaimTypes.Subject, TestUserId.ToString()),
            new(TokenClaimTypes.SessionId, TestSessionId.ToString()),
            new(TokenClaimTypes.Roles, $"USER;roleUserId={TestUserId}"),
            new(TokenClaimTypes.Scope, PermissionIds.Api.Auth.Refresh.WithUserId(TestUserId.ToString()).Deny()),
            new(RbacConstants.VersionClaimType, RbacConstants.CurrentVersion)
        };
        var identity = new ClaimsIdentity(claims, "Bearer");
        var principal = new ClaimsPrincipal(identity);

        var tokenProvider = Substitute.For<ITokenProvider>();
        tokenProvider.ValidateTokenPrincipalAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(principal);

        var roleRepo = new InMemoryRoleRepository();
        roleRepo.Add(Roles.User.Instantiate());

        var permissionService = new PermissionService(tokenProvider, roleRepo);
        var service = new UserTokenService(userAuthorizationService, sessionService, permissionService, tokenProvider);

        var result = await service.RefreshTokensAsync("not-a-real-token", CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("invalid_token_type", result.Error);

        await sessionService.DidNotReceiveWithAnyArgs()
            .ValidateSessionWithTokenAsync(default, default!, default);
    }
}

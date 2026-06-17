using Presentation.WebApp.Server.FunctionalTests;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Text.Json;

namespace Presentation.WebApp.Server.FunctionalTests.Auth;

/// <summary>
/// Tests verifying the JWT token format uses correct claim types and inline role parameters.
/// </summary>
public class TokenFormatTests : WebAppTestBase
{
    public TokenFormatTests(ITestOutputHelper output) : base(output)
    {
    }

    #region JWT Claim Type Tests

    [TimedFact]
    public async Task AccessToken_UsesShortClaimTypeNames()
    {
        Output.WriteLine("[TEST] AccessToken_UsesShortClaimTypeNames");

        var authResult = await RegisterAnonymousUserAsync();
        Assert.NotNull(authResult?.AccessToken);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(authResult!.AccessToken);

        Output.WriteLine($"[INFO] Token claims:");
        foreach (var claim in jwt.Claims)
        {
            Output.WriteLine($"  {claim.Type}: {claim.Value}");
        }

        // Verify short claim types are used (not verbose MS schemas)
        Assert.True(jwt.Claims.Any(c => c.Type == "sub"), "Should have 'sub' claim for user ID");
        Assert.True(jwt.Claims.Any(c => c.Type == "name"), "Should have 'name' claim (not verbose URL)");
        Assert.True(jwt.Claims.Any(c => c.Type == "roles"), "Should have 'roles' claim (RFC 9068)");

        // Verify verbose MS schemas are NOT present
        Assert.False(jwt.Claims.Any(c => c.Type.Contains("schemas.xmlsoap.org")), "Should NOT have verbose xmlsoap schema claims");
        Assert.False(jwt.Claims.Any(c => c.Type.Contains("schemas.microsoft.com")), "Should NOT have verbose MS schema claims");

        Output.WriteLine("[PASS] Token uses short claim type names");
    }

    [TimedFact]
    public async Task AccessToken_RoleClaimHasInlineParameters()
    {
        Output.WriteLine("[TEST] AccessToken_RoleClaimHasInlineParameters");

        var authResult = await RegisterAnonymousUserAsync();
        Assert.NotNull(authResult?.AccessToken);
        Assert.NotNull(authResult?.User?.Id);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(authResult!.AccessToken);

        var roleClaim = jwt.Claims.FirstOrDefault(c => c.Type == "roles");
        Assert.NotNull(roleClaim);
        Output.WriteLine($"[INFO] Role claim value: {roleClaim!.Value}");

        // Role claim should be in format: USER;roleUserId={userId}
        Assert.StartsWith("USER;", roleClaim.Value);
        Assert.Contains($"roleUserId={authResult.User!.Id}", roleClaim.Value);

        Output.WriteLine("[PASS] Role claim has inline parameters");
    }

    [TimedFact]
    public async Task AccessToken_DoesNotContainRoleDerivedScopes()
    {
        Output.WriteLine("[TEST] AccessToken_DoesNotContainRoleDerivedScopes");

        var authResult = await RegisterAnonymousUserAsync();
        Assert.NotNull(authResult?.AccessToken);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(authResult!.AccessToken);

        var scopeClaims = jwt.Claims.Where(c => c.Type == "scope").ToList();
        Output.WriteLine($"[INFO] Scope claims count: {scopeClaims.Count}");
        foreach (var scope in scopeClaims)
        {
            Output.WriteLine($"  scope: {scope.Value}");
        }

        // Role-derived scopes (like allow;_read;userId=xxx) should NOT be in token
        // They are resolved at runtime from the database
        Assert.DoesNotContain(scopeClaims, s => s.Value.StartsWith("allow;_read;userId="));
        Assert.DoesNotContain(scopeClaims, s => s.Value.StartsWith("allow;_write;userId="));

        Output.WriteLine("[PASS] Token does not contain role-derived scopes");
    }

    [TimedFact]
    public async Task RefreshToken_OnlyHasRefreshPermission()
    {
        Output.WriteLine("[TEST] RefreshToken_OnlyHasRefreshPermission");

        var authResult = await RegisterAnonymousUserAsync();
        Assert.NotNull(authResult?.RefreshToken);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(authResult!.RefreshToken);

        var scopeClaims = jwt.Claims.Where(c => c.Type == "scope").ToList();
        Output.WriteLine($"[INFO] Refresh token scope claims:");
        foreach (var scope in scopeClaims)
        {
            Output.WriteLine($"  scope: {scope.Value}");
        }

        // Refresh token should only have api:auth:refresh permission
        Assert.Single(scopeClaims);
        Assert.StartsWith("allow;api:auth:refresh;", scopeClaims[0].Value);

        // Refresh token should NOT have role claims
        Assert.DoesNotContain(jwt.Claims, c => c.Type == "roles");

        Output.WriteLine("[PASS] Refresh token has only refresh permission");
    }

    #endregion

    #region API Response Format Tests

    [TimedFact]
    public async Task AuthResponse_RolesHaveInlineFormat()
    {
        Output.WriteLine("[TEST] AuthResponse_RolesHaveInlineFormat");

        var authResult = await RegisterAnonymousUserAsync();
        Assert.NotNull(authResult?.User?.Roles);
        Assert.NotNull(authResult?.User?.Id);

        Output.WriteLine($"[INFO] API response roles:");
        foreach (var role in authResult!.User!.Roles!)
        {
            Output.WriteLine($"  role: {role}");
        }

        // Roles should be in inline format: USER;roleUserId={userId}
        Assert.Contains(authResult.User.Roles, r => r.StartsWith("USER;") && r.Contains($"roleUserId={authResult.User.Id}"));

        Output.WriteLine("[PASS] API response roles have inline format");
    }

    [TimedFact]
    public async Task AuthResponse_PermissionsIncludeRoleDerivedScopes()
    {
        Output.WriteLine("[TEST] AuthResponse_PermissionsIncludeRoleDerivedScopes");

        var authResult = await RegisterAnonymousUserAsync();
        Assert.NotNull(authResult?.User?.Permissions);
        Assert.NotNull(authResult?.User?.Id);

        Output.WriteLine($"[INFO] API response permissions:");
        foreach (var perm in authResult!.User!.Permissions!)
        {
            Output.WriteLine($"  permission: {perm}");
        }

        // Permissions in response should include role-derived scopes (for display)
        Assert.Contains(authResult.User.Permissions, p => p.Contains("_read;userId=" + authResult.User.Id));
        Assert.Contains(authResult.User.Permissions, p => p.Contains("_write;userId=" + authResult.User.Id));

        Output.WriteLine("[PASS] API response permissions include role-derived scopes");
    }

    #endregion

    #region RBAC Version Tests

    [TimedFact]
    public async Task AccessToken_HasRbacVersion2()
    {
        Output.WriteLine("[TEST] AccessToken_HasRbacVersion2");

        var authResult = await RegisterAnonymousUserAsync();
        Assert.NotNull(authResult?.AccessToken);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(authResult!.AccessToken);

        var rbacClaim = jwt.Claims.FirstOrDefault(c => c.Type == "rbac_version");
        Assert.NotNull(rbacClaim);
        Assert.Equal("2", rbacClaim!.Value);

        Output.WriteLine("[PASS] Token has rbac_version=2");
    }

    #endregion

    #region Helper Methods

    private async Task<AuthResponse?> RegisterAnonymousUserAsync()
    {
        var response = await HttpClient.PostAsync("/api/v1/auth/register", null);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
    }

    private sealed class AuthResponse
    {
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public string? TokenType { get; set; }
        public int ExpiresIn { get; set; }
        public UserInfo? User { get; set; }
    }

    private sealed class UserInfo
    {
        public Guid Id { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string[]? Roles { get; set; }
        public string[]? Permissions { get; set; }
        public bool IsAnonymous { get; set; }
    }

    #endregion
}





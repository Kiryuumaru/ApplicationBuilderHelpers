using Application.Server.Authorization.Interfaces.Outbound;
using Application.Server.Authorization.Models;
using Application.Server.Authorization.Services;
using Application.UnitTests.Authorization.Fakes;
using System.Linq;
using System.Security.Claims;

namespace Application.UnitTests.Authorization;

public class PermissionServiceTests
{
    // Use real permissions from the system that exist under api:auth (user-scoped)
    private const string SessionsListPermission = "api:auth:sessions:list;userId=user-123";
    private const string ApiKeysListPermission = "api:auth:api_keys:list;userId=user-123";
    // A write permission for testing write scope matching
    private const string SessionsRevokePermission = "api:auth:sessions:revoke;userId=user-123";

    [Fact]
    public async Task GenerateTokenWithPermissionsAsync_NormalizesAndDelegatesScopes()
    {
        var tokenProvider = new RecordingTokenProvider();
        var service = CreateService(tokenProvider);

        var permissions = new[]
        {
            "  " + SessionsListPermission + "  ",
            SessionsListPermission,
            ApiKeysListPermission
        };

        await service.GenerateTokenWithPermissionsAsync(
            userId: "user-123",
            username: "user@test",
            permissionIdentifiers: permissions,
            cancellationToken: CancellationToken.None);

        Assert.Equal([SessionsListPermission, ApiKeysListPermission], tokenProvider.LastGeneratedScopes);
        Assert.Equal("user-123", tokenProvider.LastGenerateTokenUserId);
        Assert.Equal("user@test", tokenProvider.LastGenerateTokenUsername);
    }

    [Fact]
    public async Task HasPermission_RejectsLegacyToken()
    {
        var service = CreateService(new RecordingTokenProvider());
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        var result = await service.HasPermissionAsync(principal, ApiKeysListPermission, CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task ValidatePermissionsAsync_AllowsAncestorScopedParameters()
    {
        var service = CreateService(new RecordingTokenProvider());

        var result = await service.ValidatePermissionsAsync(["api:_read;userId=user-123"]);

        Assert.True(result);
    }

    [Fact]
    public async Task ValidatePermissionsAsync_RejectsUnknownParameters()
    {
        var service = CreateService(new RecordingTokenProvider());

        var result = await service.ValidatePermissionsAsync(["api:_read;tenantId=abc"]);

        Assert.False(result);
    }

    [Fact]
    public async Task HasPermission_AllowsUserScopedRootGrant()
    {
        var service = CreateService(new RecordingTokenProvider());
        // New directive format: allow;permission_path;param=value
        var identity = new ClaimsIdentity(
        [
            new Claim("scope", "allow;api:_read;userId=user-123"),
            new Claim("rbac_version", "2")
        ]);
        var principal = new ClaimsPrincipal(identity);

        // Requests carry parameters in semicolon notation - use real permission under api:auth
        var permitted = "api:auth:sessions:list;userId=user-123";
        var otherUser = "api:auth:sessions:list;userId=user-456";
        // Non-scoped request without userId - DENIED because scope requires userId
        var nonScoped = "api:_read";

        Assert.True(await service.HasPermissionAsync(principal, permitted, CancellationToken.None));
        Assert.False(await service.HasPermissionAsync(principal, otherUser, CancellationToken.None));
        Assert.False(await service.HasPermissionAsync(principal, nonScoped, CancellationToken.None)); // Scope requires userId, request has none
    }

    [Fact]
    public async Task HasPermission_DeniesWhenParameterMissingOnRequest()
    {
        var service = CreateService(new RecordingTokenProvider());
        // Scope requires userId parameter
        var principal = BuildPrincipalWithScopes("allow;api:_read;userId=user-123");
        // Request has no userId parameter - the scope requires userId so this DENIES
        var permissionWithoutParameter = "api:_read";

        // The scope says "allow api:_read only for userId=user-123"
        // A request without userId doesn't satisfy this - so it's DENIED
        Assert.False(await service.HasPermissionAsync(principal, permissionWithoutParameter, CancellationToken.None));
    }

    [Fact]
    public async Task HasPermission_DeniesAccessToOtherUser()
    {
        var service = CreateService(new RecordingTokenProvider());
        // Scope requires userId=user-123
        var principal = BuildPrincipalWithScopes("allow;api:_read;userId=user-123");

        // Attempt with userId=user-456 (different user)
        var attempt = "api:auth:sessions:list;userId=user-456";

        Assert.False(await service.HasPermissionAsync(principal, attempt, CancellationToken.None));
    }

    [Fact]
    public async Task HasAnyPermission_DeniesWhenOnlyOtherUserSpecified()
    {
        var service = CreateService(new RecordingTokenProvider());
        // Scope only allows user-123
        var principal = BuildPrincipalWithScopes("allow;api:_read;userId=user-123");

        // Both requests are for user-456 (different user)
        var otherUser = "api:auth:sessions:list;userId=user-456";
        var otherUserApiKeys = "api:auth:api_keys:list;userId=user-456";

        Assert.False(await service.HasAnyPermissionAsync(principal, [otherUser, otherUserApiKeys], CancellationToken.None));
    }

    [Fact]
    public async Task HasPermission_WriteScopeAllowsWriteOperations()
    {
        var service = CreateService(new RecordingTokenProvider());
        // Use new directive format with _write scope
        var principal = BuildPrincipalWithScopes("allow;api:_write;userId=user-123");

        var result = await service.HasPermissionAsync(principal, SessionsRevokePermission, CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task HasPermission_ReadScopeDoesNotAllowWriteOperations()
    {
        var service = CreateService(new RecordingTokenProvider());
        // Only read scope, no write
        var principal = BuildPrincipalWithScopes("allow;api:_read;userId=user-123");

        var result = await service.HasPermissionAsync(principal, SessionsRevokePermission, CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task HasAllPermissions_ReturnsFalseWhenAnyMissing()
    {
        var service = CreateService(new RecordingTokenProvider());
        // Only allows sessions:list path, not api_keys:list
        var identity = new ClaimsIdentity(
        [
            new Claim("scope", "allow;api:auth:sessions:list;userId=user-123"),
            new Claim("rbac_version", "2")
        ]);
        var principal = new ClaimsPrincipal(identity);

        var result = await service.HasAllPermissionsAsync(principal, [SessionsListPermission, ApiKeysListPermission], CancellationToken.None);

        Assert.False(result);
    }

    private static ClaimsPrincipal BuildPrincipalWithScopes(params string[] scopes)
    {
        var scopeValue = string.Join(' ', scopes);
        var identity = new ClaimsIdentity(
        [
            new Claim("scope", scopeValue),
            new Claim("rbac_version", "2")
        ]);

        return new ClaimsPrincipal(identity);
    }

    private static ClaimsPrincipal BuildLegacyPrincipalWithScopes(params string[] scopes)
    {
        var scopeValue = string.Join(' ', scopes);
        var identity = new ClaimsIdentity(
        [
            new Claim("scope", scopeValue),
            new Claim("rbac_version", "1")
        ]);

        return new ClaimsPrincipal(identity);
    }

    private static PermissionService CreateService(RecordingTokenProvider tokenProvider)
        => new(tokenProvider, new InMemoryRoleRepository());

    private sealed class RecordingTokenProvider : ITokenProvider
    {
        public string GenerateTokenResult { get; set; } = "token";
        public string GenerateApiKeyTokenResult { get; set; } = "api-token";
        public string MutateTokenResult { get; set; } = "mutated-token";
        public ClaimsPrincipal? ValidateTokenPrincipalResult { get; set; }
        public TokenInfo? DecodeTokenResult { get; set; }

        public string? LastGenerateTokenUserId { get; private set; }
        public string? LastGenerateTokenUsername { get; private set; }
        public IReadOnlyCollection<string>? LastGeneratedScopes { get; private set; }
        public string? LastApiKeyName { get; private set; }
        public IReadOnlyCollection<string>? LastApiKeyScopes { get; private set; }
        public string? LastMutatedToken { get; private set; }
        public IReadOnlyCollection<string>? LastMutateScopesToAdd { get; private set; }
        public IReadOnlyCollection<string>? LastMutateScopesToRemove { get; private set; }

        public Task<string> GenerateTokenWithScopesAsync(string userId, string? username, IEnumerable<string> scopes, IEnumerable<Claim>? additionalClaims = null, DateTimeOffset? expiration = null, Domain.Identity.Enums.TokenType tokenType = Domain.Identity.Enums.TokenType.Access, string? tokenId = null, CancellationToken cancellationToken = default)
        {
            LastGenerateTokenUserId = userId;
            LastGenerateTokenUsername = username;
            LastGeneratedScopes = scopes?.ToArray();
            return Task.FromResult(GenerateTokenResult);
        }

        public Task<string> GenerateApiKeyTokenAsync(string apiKeyName, IEnumerable<string>? scopes = null, IEnumerable<Claim>? additionalClaims = null, DateTimeOffset? expiration = null, CancellationToken cancellationToken = default)
        {
            LastApiKeyName = apiKeyName;
            LastApiKeyScopes = scopes?.ToArray();
            return Task.FromResult(GenerateApiKeyTokenResult);
        }

        public Task<ClaimsPrincipal?> ValidateTokenPrincipalAsync(string token, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ValidateTokenPrincipalResult);
        }

        public Task<TokenInfo?> DecodeTokenAsync(string token, CancellationToken cancellationToken = default)
            => Task.FromResult(DecodeTokenResult);

        public Task<string> MutateTokenAsync(string token, IEnumerable<string>? scopesToAdd = null, IEnumerable<string>? scopesToRemove = null, IEnumerable<Claim>? claimsToAdd = null, IEnumerable<Claim>? claimsToRemove = null, IEnumerable<string>? claimTypesToRemove = null, DateTimeOffset? expiration = null, CancellationToken cancellationToken = default)
        {
            LastMutatedToken = token;
            LastMutateScopesToAdd = scopesToAdd?.ToArray();
            LastMutateScopesToRemove = scopesToRemove?.ToArray();
            return Task.FromResult(MutateTokenResult);
        }
    }
}

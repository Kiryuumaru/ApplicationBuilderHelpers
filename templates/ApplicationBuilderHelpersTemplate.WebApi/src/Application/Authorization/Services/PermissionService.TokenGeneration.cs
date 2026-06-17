using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Domain.Authorization.ValueObjects;
using Domain.Identity.Enums;

namespace Application.Authorization.Services;

internal sealed partial class PermissionService
{
    public async Task<string> GenerateTokenWithPermissionsAsync(
        string userId,
        string? username,
        IEnumerable<string> permissionIdentifiers,
        IEnumerable<Claim>? additionalClaims = null,
        DateTimeOffset? expiration = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(userId);

        var normalizedPermissions = NormalizeAndValidate(permissionIdentifiers, allowEmpty: true);
        var additionalClaimSet = additionalClaims?.ToArray();

        return await tokenProvider.GenerateTokenWithScopesAsync(
            userId: userId,
            username: username ?? string.Empty,
            scopes: normalizedPermissions,
            additionalClaims: additionalClaimSet,
            expiration: expiration,
            cancellationToken: cancellationToken);
    }

    public async Task<string> GenerateApiKeyTokenWithPermissionsAsync(
        string apiKeyName,
        IEnumerable<string> permissionIdentifiers,
        IEnumerable<Claim>? additionalClaims = null,
        DateTimeOffset? expiration = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(apiKeyName);

        var normalizedPermissions = NormalizeAndValidate(permissionIdentifiers, allowEmpty: true);
        var additionalClaimSet = additionalClaims?.ToArray();

        return await tokenProvider.GenerateApiKeyTokenAsync(
            apiKeyName: apiKeyName,
            scopes: normalizedPermissions,
            additionalClaims: additionalClaimSet,
            expiration: expiration,
            cancellationToken: cancellationToken);
    }

    public async Task<string> GenerateTokenWithScopeAsync(
        string userId,
        string? username,
        IEnumerable<ScopeDirective> scopeDirectives,
        IEnumerable<Claim>? additionalClaims = null,
        DateTimeOffset? expiration = null,
        TokenType tokenType = TokenType.Access,
        string? tokenId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(userId);

        // Convert scope directives to their string representation
        var scopes = scopeDirectives?
            .Where(static d => d != null)
            .Select(static d => d.ToString())
            .ToArray() ?? Array.Empty<string>();

        var additionalClaimSet = additionalClaims?.ToArray();

        return await tokenProvider.GenerateTokenWithScopesAsync(
            userId: userId,
            username: username ?? string.Empty,
            scopes: scopes,
            additionalClaims: additionalClaimSet,
            expiration: expiration,
            tokenType: tokenType,
            tokenId: tokenId,
            cancellationToken: cancellationToken);
    }
}

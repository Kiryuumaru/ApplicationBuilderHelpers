using Application.Server.Authorization.Interfaces.Inbound;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Server.Authorization.Extensions;

public static class IPermissionServiceExtensions
{
    public static Task<string> GenerateTokenWithPermissionsAsync(
        this IPermissionService permissionService,
        string userId,
        string username,
        IEnumerable<string> permissionIdentifiers,
        IEnumerable<Claim>? additionalClaims = null,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default) =>
        permissionService.GenerateTokenWithPermissionsAsync(
            userId: userId,
            username: username,
            permissionIdentifiers: permissionIdentifiers,
            additionalClaims: additionalClaims,
            expiration: expiration.HasValue ? DateTimeOffset.UtcNow.Add(expiration.Value) : null,
            cancellationToken: cancellationToken);

    public static Task<string> GenerateApiKeyTokenWithPermissionsAsync(
        this IPermissionService permissionService,
        string apiKeyName,
        IEnumerable<string> permissionIdentifiers,
        IEnumerable<Claim>? additionalClaims = null,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default) =>
        permissionService.GenerateApiKeyTokenWithPermissionsAsync(
            apiKeyName: apiKeyName,
            permissionIdentifiers: permissionIdentifiers,
            additionalClaims: additionalClaims,
            expiration: expiration.HasValue ? DateTimeOffset.UtcNow.Add(expiration.Value) : null,
            cancellationToken: cancellationToken);
}

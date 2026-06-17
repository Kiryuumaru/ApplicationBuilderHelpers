using Domain.Identity.Enums;
using Infrastructure.Server.Identity.Interfaces;
using System.Security.Claims;

namespace Infrastructure.Server.Identity.Extensions;

internal static class IJwtTokenServiceExtensions
{
    public static Task<string> GenerateToken(
        this IJwtTokenService jwtTokenService,
        string userId,
        string username,
        IEnumerable<string>? scopes = null,
        IEnumerable<Claim>? additionalClaims = null,
        TimeSpan? expiration = null,
        TokenType tokenType = TokenType.Access,
        CancellationToken cancellationToken = default) =>
        jwtTokenService.GenerateToken(
            userId: userId,
            username: username,
            scopes: scopes,
            additionalClaims: additionalClaims,
            expiration: expiration.HasValue ? DateTimeOffset.UtcNow.Add(expiration.Value) : null,
            tokenType: tokenType,
            cancellationToken: cancellationToken);

    public static Task<string> MutateToken(
        this IJwtTokenService jwtTokenService,
        string token,
        IEnumerable<string>? scopesToAdd = null,
        IEnumerable<string>? scopesToRemove = null,
        IEnumerable<Claim>? claimsToAdd = null,
        IEnumerable<Claim>? claimsToRemove = null,
        IEnumerable<string>? claimTypesToRemove = null,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default) =>
        jwtTokenService.MutateToken(
            token: token,
            scopesToAdd: scopesToAdd,
            scopesToRemove: scopesToRemove,
            claimsToAdd: claimsToAdd,
            claimsToRemove: claimsToRemove,
            claimTypesToRemove: claimTypesToRemove,
            expiration: expiration.HasValue ? (DateTimeOffset?)DateTimeOffset.UtcNow.Add(expiration.Value) : null,
            cancellationToken: cancellationToken);
}

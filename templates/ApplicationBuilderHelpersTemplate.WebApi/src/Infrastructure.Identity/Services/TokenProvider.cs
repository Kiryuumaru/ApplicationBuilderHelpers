using Application.Authorization.Interfaces.Outbound;
using Application.Authorization.Models;
using Domain.Identity.Enums;
using Infrastructure.Identity.Interfaces;
using System.Security.Claims;

namespace Infrastructure.Identity.Services;

internal sealed class TokenProvider(IJwtTokenService jwtTokenService) : ITokenProvider
{
    public async Task<string> GenerateTokenWithScopesAsync(
        string userId,
        string? username,
        IEnumerable<string> scopes,
        IEnumerable<Claim>? additionalClaims = null,
        DateTimeOffset? expiration = null,
        TokenType tokenType = TokenType.Access,
        string? tokenId = null,
        CancellationToken cancellationToken = default)
    {
        return await jwtTokenService.GenerateToken(
            userId: userId,
            username: username ?? userId,
            scopes: scopes,
            additionalClaims: additionalClaims,
            expiration: expiration,
            tokenType: tokenType,
            tokenId: tokenId,
            cancellationToken: cancellationToken);
    }

    public async Task<string> GenerateApiKeyTokenAsync(
        string apiKeyName,
        IEnumerable<string>? scopes = null,
        IEnumerable<Claim>? additionalClaims = null,
        DateTimeOffset? expiration = null,
        CancellationToken cancellationToken = default)
    {
        var claims = new List<Claim>
        {
            new("api_key", "true")
        };

        if (additionalClaims is not null)
        {
            claims.AddRange(additionalClaims);
        }

        return await jwtTokenService.GenerateToken(
            userId: apiKeyName,
            username: apiKeyName,
            scopes: scopes,
            additionalClaims: claims,
            expiration: expiration,
            tokenType: TokenType.ApiKey,
            cancellationToken: cancellationToken);
    }

    public async Task<ClaimsPrincipal?> ValidateTokenPrincipalAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        return await jwtTokenService.ValidateToken(token, expectedType: null, cancellationToken);
    }

    public async Task<TokenInfo?> DecodeTokenAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        return await jwtTokenService.DecodeToken(token, cancellationToken);
    }

    public async Task<string> MutateTokenAsync(
        string token,
        IEnumerable<string>? scopesToAdd = null,
        IEnumerable<string>? scopesToRemove = null,
        IEnumerable<Claim>? claimsToAdd = null,
        IEnumerable<Claim>? claimsToRemove = null,
        IEnumerable<string>? claimTypesToRemove = null,
        DateTimeOffset? expiration = null,
        CancellationToken cancellationToken = default)
    {
        return await jwtTokenService.MutateToken(
            token: token,
            scopesToAdd: scopesToAdd,
            scopesToRemove: scopesToRemove,
            claimsToAdd: claimsToAdd,
            claimsToRemove: claimsToRemove,
            claimTypesToRemove: claimTypesToRemove,
            expiration: expiration,
            cancellationToken: cancellationToken);
    }
}

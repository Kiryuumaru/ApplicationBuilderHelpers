using Application.Authorization.Models;
using Domain.Identity.Enums;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace Infrastructure.Identity.Interfaces;

/// <summary>
/// Internal interface for JWT token operations.
/// This is an infrastructure interface, implemented by services that handle JWT details.
/// </summary>
internal interface IJwtTokenService
{
    /// <summary>
    /// Generates a JWT token for the specified user.
    /// </summary>
    /// <param name="userId">The user ID (sub claim).</param>
    /// <param name="username">The username (name claim).</param>
    /// <param name="scopes">Optional scope claims for permissions.</param>
    /// <param name="additionalClaims">Optional additional claims to include.</param>
    /// <param name="expiration">Optional expiration override.</param>
    /// <param name="tokenType">The type of token to generate (sets typ header).</param>
    /// <param name="tokenId">Optional custom token ID (jti claim). If null, a random GUID is used.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<string> GenerateToken(
        string userId,
        string username,
        IEnumerable<string>? scopes = null,
        IEnumerable<Claim>? additionalClaims = null,
        DateTimeOffset? expiration = null,
        TokenType tokenType = TokenType.Access,
        string? tokenId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a JWT token and returns the claims principal.
    /// </summary>
    /// <param name="token">The token to validate.</param>
    /// <param name="expectedType">If specified, validates the typ header matches. If null, skips typ validation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<ClaimsPrincipal?> ValidateToken(
        string token,
        TokenType? expectedType = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Decodes a JWT token without validation.
    /// </summary>
    Task<TokenInfo?> DecodeToken(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mutates an existing token by adding/removing scopes and claims.
    /// </summary>
    Task<string> MutateToken(
        string token,
        IEnumerable<string>? scopesToAdd = null,
        IEnumerable<string>? scopesToRemove = null,
        IEnumerable<Claim>? claimsToAdd = null,
        IEnumerable<Claim>? claimsToRemove = null,
        IEnumerable<string>? claimTypesToRemove = null,
        DateTimeOffset? expiration = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the token validation parameters for validating JWTs.
    /// </summary>
    Task<TokenValidationParameters> GetTokenValidationParameters(CancellationToken cancellationToken = default);
}

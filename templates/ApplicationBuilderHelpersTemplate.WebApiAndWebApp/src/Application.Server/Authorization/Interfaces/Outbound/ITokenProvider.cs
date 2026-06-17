using Application.Server.Authorization.Models;
using System.Security.Claims;

namespace Application.Server.Authorization.Interfaces.Outbound;

/// <summary>
/// Provider for token generation and validation operations.
/// Abstracts JWT implementation details behind an Application-owned port.
/// Implemented by Infrastructure layer.
/// </summary>
public interface ITokenProvider
{
    /// <summary>
    /// Generates a token with the specified permissions/scopes.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="username">The username (null for anonymous users).</param>
    /// <param name="scopes">The permission scopes to include.</param>
    /// <param name="additionalClaims">Additional claims to include.</param>
    /// <param name="expiration">Optional expiration override.</param>
    /// <param name="tokenType">The type of token to generate (Access, Refresh, or ApiKey).</param>
    /// <param name="tokenId">Optional custom token ID (jti claim). If null, a random GUID is used.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated token.</returns>
    Task<string> GenerateTokenWithScopesAsync(
        string userId,
        string? username,
        IEnumerable<string> scopes,
        IEnumerable<Claim>? additionalClaims = null,
        DateTimeOffset? expiration = null,
        Domain.Identity.Enums.TokenType tokenType = Domain.Identity.Enums.TokenType.Access,
        string? tokenId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates an API key token with the specified permissions/scopes.
    /// </summary>
    /// <param name="apiKeyName">The API key name.</param>
    /// <param name="scopes">The permission scopes to include.</param>
    /// <param name="additionalClaims">Additional claims to include.</param>
    /// <param name="expiration">Optional expiration override.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated API key token.</returns>
    Task<string> GenerateApiKeyTokenAsync(
        string apiKeyName,
        IEnumerable<string>? scopes = null,
        IEnumerable<Claim>? additionalClaims = null,
        DateTimeOffset? expiration = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a token and returns the claims principal.
    /// </summary>
    /// <param name="token">The token to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The claims principal if valid, null otherwise.</returns>
    Task<ClaimsPrincipal?> ValidateTokenPrincipalAsync(
        string token,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Decodes a token without validation.
    /// </summary>
    /// <param name="token">The token to decode.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The token info if decodable, null otherwise.</returns>
    Task<TokenInfo?> DecodeTokenAsync(
        string token,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Mutates an existing token by adding/removing scopes and claims.
    /// </summary>
    /// <param name="token">The token to mutate.</param>
    /// <param name="scopesToAdd">Scopes to add.</param>
    /// <param name="scopesToRemove">Scopes to remove.</param>
    /// <param name="claimsToAdd">Claims to add.</param>
    /// <param name="claimsToRemove">Claims to remove.</param>
    /// <param name="claimTypesToRemove">Claim types to remove entirely.</param>
    /// <param name="expiration">Optional new expiration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The mutated token.</returns>
    Task<string> MutateTokenAsync(
        string token,
        IEnumerable<string>? scopesToAdd = null,
        IEnumerable<string>? scopesToRemove = null,
        IEnumerable<Claim>? claimsToAdd = null,
        IEnumerable<Claim>? claimsToRemove = null,
        IEnumerable<string>? claimTypesToRemove = null,
        DateTimeOffset? expiration = null,
        CancellationToken cancellationToken = default);
}

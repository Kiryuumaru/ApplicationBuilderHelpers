using System.Security.Claims;
using Application.Server.Identity.Enums;
using Application.Server.Identity.Interfaces.Inbound;
using Application.Server.Identity.Models;
using Domain.Identity.Constants;

namespace Application.Server.Identity.Services;

internal sealed class TokenValidationService(
    ISessionService sessionService,
    IApiKeyService apiKeyService) : ITokenValidationService
{
    public async Task<TokenValidationResult> ValidatePostSignatureAsync(
        ClaimsPrincipal principal,
        string? typHeader,
        TokenType[] allowedTypes,
        CancellationToken cancellationToken)
    {

        // 2. Map typ header to TokenType
        var tokenType = typHeader switch
        {
            TokenClaimTypes.TokenTypeValues.AccessToken => TokenType.Access,
            TokenClaimTypes.TokenTypeValues.RefreshToken => TokenType.Refresh,
            TokenClaimTypes.TokenTypeValues.ApiKey => TokenType.ApiKey,
            _ => (TokenType?)null
        };

        if (tokenType is null)
        {
            return TokenValidationResult.Failure($"Unknown token type: {typHeader ?? "null"}");
        }

        // 3. Check if this token type is allowed for this endpoint
        if (!allowedTypes.Contains(tokenType.Value))
        {
            return TokenValidationResult.Failure($"Token type '{typHeader}' is not allowed for this endpoint");
        }

        // 4. Type-specific post-validation
        switch (tokenType.Value)
        {
            case TokenType.Access:
                return await ValidateAccessTokenAsync(principal, cancellationToken);

            case TokenType.Refresh:
                // Refresh tokens need session validation to ensure the session hasn't been revoked.
                // Note: This allows refresh tokens to pass authentication, but they will still
                // fail authorization (403) on protected endpoints because they lack permission claims.
                return await ValidateRefreshTokenAsync(principal, cancellationToken);

            case TokenType.ApiKey:
                return await ValidateApiKeyTokenAsync(principal, cancellationToken);

            default:
                return TokenValidationResult.Failure("Unsupported token type");
        }
    }

    /// <summary>
    /// Validates an access token by checking if the session is still valid.
    /// </summary>
    private async Task<TokenValidationResult> ValidateAccessTokenAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        // Extract session ID from claims
        var sessionIdClaim = principal.FindFirst(TokenClaimTypes.SessionId);
        if (sessionIdClaim is null || !Guid.TryParse(sessionIdClaim.Value, out var sessionId))
        {
            return TokenValidationResult.Failure("Token is missing required session identifier");
        }

        // Validate session is still active
        var session = await sessionService.GetByIdAsync(sessionId, cancellationToken);
        if (session is null || !session.IsValid)
        {
            return TokenValidationResult.Failure("Session has been revoked or is no longer valid");
        }

        return TokenValidationResult.Success(TokenType.Access);
    }

    /// <summary>
    /// Validates an API key token by checking if it's revoked in the database.
    /// </summary>
    private async Task<TokenValidationResult> ValidateApiKeyTokenAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        // Extract jti (token ID) which is the API key ID for revocation lookup
        var jtiClaim = principal.FindFirst(TokenClaimTypes.TokenId);
        if (jtiClaim is null || !Guid.TryParse(jtiClaim.Value, out var keyId))
        {
            return TokenValidationResult.Failure("API key token is missing jti claim");
        }

        // Validate API key is not revoked
        var apiKey = await apiKeyService.ValidateApiKeyAsync(keyId, cancellationToken);
        if (apiKey is null)
        {
            return TokenValidationResult.Failure("API key has been revoked or not found");
        }

        // Fire-and-forget last used update (don't await, don't block the request)
        _ = Task.Run(async () =>
        {
            try
            {
                await apiKeyService.UpdateLastUsedAsync(keyId, CancellationToken.None);
            }
            catch
            {
                // Ignore errors in background update - not critical
            }
        }, CancellationToken.None);

        return TokenValidationResult.Success(TokenType.ApiKey);
    }

    /// <summary>
    /// Validates a refresh token by checking if the session is still valid.
    /// This allows refresh tokens to pass authentication, but they will fail
    /// authorization on protected endpoints because they lack permission claims.
    /// </summary>
    private async Task<TokenValidationResult> ValidateRefreshTokenAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        // Extract session ID from claims
        var sessionIdClaim = principal.FindFirst(TokenClaimTypes.SessionId);
        if (sessionIdClaim is null || !Guid.TryParse(sessionIdClaim.Value, out var sessionId))
        {
            return TokenValidationResult.Failure("Token is missing required session identifier");
        }

        // Validate session is still active
        var session = await sessionService.GetByIdAsync(sessionId, cancellationToken);
        if (session is null || !session.IsValid)
        {
            return TokenValidationResult.Failure("session_revoked");
        }

        return TokenValidationResult.Success(TokenType.Refresh);
    }
}

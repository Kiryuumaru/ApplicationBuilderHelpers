using System.Security.Claims;
using Domain.Identity.Enums;
using Application.Identity.Models;

namespace Application.Identity.Interfaces.Inbound;

/// <summary>
/// Unified token validation service for all JWT types (access, refresh, API key).
/// This is the post-signature validation orchestrator that routes to appropriate
/// validation based on the token's typ header.
/// </summary>
public interface ITokenValidationService
{
    /// <summary>
    /// Validates a token AFTER signature verification by middleware.
    /// Routes to appropriate validation based on typ header:
    /// - <see cref="TokenType.Access"/> → Session validation via ISessionService
    /// - <see cref="TokenType.ApiKey"/> → API key revocation check via IApiKeyService
    /// - <see cref="TokenType.Refresh"/> → Allowed only for specific endpoints
    /// </summary>
    /// <param name="principal">The claims principal from middleware.</param>
    /// <param name="typHeader">The typ header value from the token.</param>
    /// <param name="allowedTypes">Which token types are accepted for this endpoint.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The validation result indicating success/failure and token type.</returns>
    Task<TokenValidationResult> ValidatePostSignatureAsync(
        ClaimsPrincipal principal,
        string? typHeader,
        TokenType[] allowedTypes,
        CancellationToken cancellationToken);
}

using System.Security.Claims;
using Domain.Identity.Constants;

namespace Presentation.WebApi.Extensions;

/// <summary>
/// Extension methods for reading common JWT claims from <see cref="ClaimsPrincipal"/>.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Gets the user ID from JWT claims.
    /// </summary>
    /// <param name="user">The claims principal.</param>
    /// <returns>User ID when present and valid; otherwise null.</returns>
    public static Guid? GetUserId(this ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(TokenClaimTypes.Subject);
        if (userIdClaim is not null && Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }
        return null;
    }

    /// <summary>
    /// Gets the username from JWT claims.
    /// </summary>
    /// <param name="user">The claims principal.</param>
    /// <returns>Username when present; otherwise null.</returns>
    public static string? GetUsername(this ClaimsPrincipal user)
    {
        return user.FindFirst(TokenClaimTypes.Name)?.Value;
    }

    /// <summary>
    /// Gets the session ID from JWT claims.
    /// </summary>
    /// <param name="user">The claims principal.</param>
    /// <returns>Session ID when present and valid; otherwise null.</returns>
    public static Guid? GetSessionId(this ClaimsPrincipal user)
    {
        var sessionIdClaim = user.FindFirst(TokenClaimTypes.SessionId);
        if (sessionIdClaim is not null && Guid.TryParse(sessionIdClaim.Value, out var sessionId))
        {
            return sessionId;
        }
        return null;
    }
}

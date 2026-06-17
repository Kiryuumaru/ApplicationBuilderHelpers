namespace Application.Identity.Models;

/// <summary>
/// Result of token generation containing access token, refresh token, and session ID.
/// </summary>
public sealed record UserTokenResult(
    string AccessToken,
    string RefreshToken,
    Guid SessionId,
    int ExpiresInSeconds);

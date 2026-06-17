namespace Application.Server.Identity.Enums;

/// <summary>
/// Represents the type of JWT token.
/// </summary>
public enum TokenType
{
    /// <summary>
    /// Access token. Short-lived, for interactive API calls.
    /// </summary>
    Access,

    /// <summary>
    /// Refresh token. Long-lived, for obtaining new access tokens.
    /// </summary>
    Refresh,

    /// <summary>
    /// API key token. Configurable lifetime, for programmatic access.
    /// </summary>
    ApiKey
}

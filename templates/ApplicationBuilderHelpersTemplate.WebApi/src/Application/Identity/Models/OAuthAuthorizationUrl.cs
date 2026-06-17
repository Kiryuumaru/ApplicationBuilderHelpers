namespace Application.Identity.Models;

/// <summary>
/// OAuth authorization URL and state for redirecting the user.
/// </summary>
public sealed record OAuthAuthorizationUrl
{
    /// <summary>
    /// The URL to redirect the user to for OAuth authorization.
    /// </summary>
    public required string AuthorizationUrl { get; init; }

    /// <summary>
    /// State parameter to prevent CSRF attacks. Should be stored in session/cookie.
    /// </summary>
    public required string State { get; init; }
}

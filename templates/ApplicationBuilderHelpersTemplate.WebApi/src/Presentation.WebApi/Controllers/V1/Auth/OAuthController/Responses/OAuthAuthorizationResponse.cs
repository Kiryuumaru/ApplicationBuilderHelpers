namespace Presentation.WebApi.Controllers.V1.Auth.OAuthController.Responses;

/// <summary>
/// Response containing OAuth authorization URL.
/// </summary>
public sealed record OAuthAuthorizationResponse
{
    /// <summary>
    /// The URL to redirect the user to for OAuth authorization.
    /// </summary>
    public required string AuthorizationUrl { get; init; }

    /// <summary>
    /// State parameter to store in session for CSRF protection.
    /// Must be passed back in the callback request.
    /// </summary>
    public required string State { get; init; }
}

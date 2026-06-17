using System.ComponentModel.DataAnnotations;

namespace Presentation.WebApp.Server.Controllers.V1.Auth.OAuthController.Requests;

/// <summary>
/// Request to process OAuth callback.
/// </summary>
public sealed record OAuthCallbackRequest
{
    /// <summary>
    /// The OAuth provider that called back.
    /// </summary>
    [Required]
    public required string Provider { get; init; }

    /// <summary>
    /// The authorization code from the OAuth provider.
    /// </summary>
    [Required]
    public required string Code { get; init; }

    /// <summary>
    /// The state parameter for CSRF protection.
    /// </summary>
    [Required]
    public required string State { get; init; }

    /// <summary>
    /// The redirect URI used in the original authorization request.
    /// </summary>
    [Required]
    public required string RedirectUri { get; init; }
}

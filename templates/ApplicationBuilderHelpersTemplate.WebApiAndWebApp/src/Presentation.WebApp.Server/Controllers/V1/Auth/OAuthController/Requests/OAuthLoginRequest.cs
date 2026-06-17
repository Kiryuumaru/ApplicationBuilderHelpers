using System.ComponentModel.DataAnnotations;

namespace Presentation.WebApp.Server.Controllers.V1.Auth.OAuthController.Requests;

/// <summary>
/// Request to initiate OAuth login flow.
/// </summary>
public sealed record OAuthLoginRequest
{
    /// <summary>
    /// The OAuth provider to use (e.g., "google", "github", "microsoft", "discord", "mock").
    /// </summary>
    [Required]
    public required string Provider { get; init; }

    /// <summary>
    /// The URL to redirect to after OAuth completes. Must be a registered callback URL.
    /// </summary>
    [Required]
    public required string RedirectUri { get; init; }
}

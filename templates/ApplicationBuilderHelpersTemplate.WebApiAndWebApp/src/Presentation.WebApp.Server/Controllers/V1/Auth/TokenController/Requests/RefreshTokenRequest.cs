using System.ComponentModel.DataAnnotations;

namespace Presentation.WebApp.Server.Controllers.V1.Auth.TokenController.Requests;

/// <summary>
/// Request model for token refresh.
/// </summary>
public sealed record RefreshTokenRequest
{
    /// <summary>
    /// The refresh token to exchange for a new access token.
    /// </summary>
    [Required]
    public required string RefreshToken { get; init; }
}

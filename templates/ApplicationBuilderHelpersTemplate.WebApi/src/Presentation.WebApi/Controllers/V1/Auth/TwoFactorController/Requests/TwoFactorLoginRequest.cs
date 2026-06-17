using System.ComponentModel.DataAnnotations;

namespace Presentation.WebApi.Controllers.V1.Auth.TwoFactorController.Requests;

/// <summary>
/// Request model for completing two-factor authentication during login.
/// </summary>
public sealed record TwoFactorLoginRequest
{
    /// <summary>
    /// The user ID received from the initial login attempt that required 2FA.
    /// </summary>
    [Required]
    public required Guid UserId { get; init; }

    /// <summary>
    /// The 6-digit code from the authenticator app or a recovery code.
    /// </summary>
    [Required]
    public required string Code { get; init; }
}

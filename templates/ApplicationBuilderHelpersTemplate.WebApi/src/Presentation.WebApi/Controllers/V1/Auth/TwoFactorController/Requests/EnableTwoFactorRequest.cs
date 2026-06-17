using System.ComponentModel.DataAnnotations;

namespace Presentation.WebApi.Controllers.V1.Auth.TwoFactorController.Requests;

/// <summary>
/// Request model for enabling two-factor authentication.
/// </summary>
public sealed record EnableTwoFactorRequest
{
    /// <summary>
    /// The 6-digit verification code from the authenticator app.
    /// </summary>
    [Required]
    [StringLength(6, MinimumLength = 6)]
    public required string VerificationCode { get; init; }
}

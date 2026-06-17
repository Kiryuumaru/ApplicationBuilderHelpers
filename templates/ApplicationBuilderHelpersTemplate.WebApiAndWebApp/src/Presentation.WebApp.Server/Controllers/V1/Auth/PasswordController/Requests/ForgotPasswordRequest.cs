using System.ComponentModel.DataAnnotations;

namespace Presentation.WebApp.Server.Controllers.V1.Auth.PasswordController.Requests;

/// <summary>
/// Request model for initiating password reset.
/// </summary>
public sealed record ForgotPasswordRequest
{
    /// <summary>
    /// The email address associated with the account.
    /// </summary>
    [Required]
    [EmailAddress]
    public required string Email { get; init; }
}

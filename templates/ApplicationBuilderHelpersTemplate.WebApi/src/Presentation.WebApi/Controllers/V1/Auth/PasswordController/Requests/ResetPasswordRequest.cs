using System.ComponentModel.DataAnnotations;

namespace Presentation.WebApi.Controllers.V1.Auth.PasswordController.Requests;

/// <summary>
/// Request model for resetting password with a token.
/// </summary>
public sealed record ResetPasswordRequest
{
    /// <summary>
    /// The email address associated with the account.
    /// </summary>
    [Required]
    [EmailAddress]
    public required string Email { get; init; }

    /// <summary>
    /// The password reset token received via email.
    /// </summary>
    [Required]
    public required string Token { get; init; }

    /// <summary>
    /// The new password.
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 8)]
    public required string NewPassword { get; init; }

    /// <summary>
    /// New password confirmation - must match NewPassword.
    /// </summary>
    [Required]
    [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
    public required string ConfirmPassword { get; init; }
}

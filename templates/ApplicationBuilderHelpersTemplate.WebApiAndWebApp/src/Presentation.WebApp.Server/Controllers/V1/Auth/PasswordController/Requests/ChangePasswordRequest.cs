using System.ComponentModel.DataAnnotations;

namespace Presentation.WebApp.Server.Controllers.V1.Auth.PasswordController.Requests;

/// <summary>
/// Request model for changing the authenticated user's password.
/// </summary>
public sealed record ChangePasswordRequest
{
    /// <summary>
    /// The user's current password for verification.
    /// </summary>
    [Required]
    public required string CurrentPassword { get; init; }

    /// <summary>
    /// The new password to set.
    /// </summary>
    [Required]
    [MinLength(8)]
    public required string NewPassword { get; init; }
}

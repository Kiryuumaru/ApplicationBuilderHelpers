using System.ComponentModel.DataAnnotations;

namespace Presentation.WebApp.Server.Controllers.V1.Iam.UsersController.Requests;

/// <summary>
/// Request to reset a user's password (admin operation).
/// </summary>
public sealed class AdminResetPasswordRequest
{
    /// <summary>
    /// Gets or sets the new password.
    /// </summary>
    [Required]
    [MinLength(8)]
    public required string NewPassword { get; init; }
}

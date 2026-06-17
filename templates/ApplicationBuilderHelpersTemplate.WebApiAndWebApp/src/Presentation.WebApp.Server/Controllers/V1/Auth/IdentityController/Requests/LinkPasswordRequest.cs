using System.ComponentModel.DataAnnotations;

namespace Presentation.WebApp.Server.Controllers.V1.Auth.IdentityController.Requests;

/// <summary>
/// Request model for linking a password to an account.
/// </summary>
public sealed record LinkPasswordRequest
{
    /// <summary>
    /// The username to set (required when upgrading from anonymous).
    /// </summary>
    [Required]
    [StringLength(50, MinimumLength = 3)]
    public required string Username { get; init; }

    /// <summary>
    /// The email to link (optional).
    /// </summary>
    [EmailAddress]
    public string? Email { get; init; }

    /// <summary>
    /// The password to set.
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 8)]
    public required string Password { get; init; }

    /// <summary>
    /// Password confirmation - must match Password.
    /// </summary>
    [Required]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    public required string ConfirmPassword { get; init; }
}

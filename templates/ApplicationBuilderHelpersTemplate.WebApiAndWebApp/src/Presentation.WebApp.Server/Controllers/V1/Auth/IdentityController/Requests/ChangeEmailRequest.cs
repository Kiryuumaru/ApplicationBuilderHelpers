using System.ComponentModel.DataAnnotations;

namespace Presentation.WebApp.Server.Controllers.V1.Auth.IdentityController.Requests;

/// <summary>
/// Request to change the current user's email address.
/// </summary>
public sealed record ChangeEmailRequest
{
    /// <summary>
    /// The new email address.
    /// </summary>
    [Required]
    [EmailAddress]
    public required string Email { get; init; }
}

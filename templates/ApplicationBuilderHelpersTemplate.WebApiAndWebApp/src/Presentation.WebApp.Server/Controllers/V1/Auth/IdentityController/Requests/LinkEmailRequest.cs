using System.ComponentModel.DataAnnotations;

namespace Presentation.WebApp.Server.Controllers.V1.Auth.IdentityController.Requests;

/// <summary>
/// Request to link an email to the current user's account.
/// </summary>
public sealed record LinkEmailRequest
{
    /// <summary>
    /// The email address to link to the account.
    /// </summary>
    [Required]
    [EmailAddress]
    public required string Email { get; init; }
}

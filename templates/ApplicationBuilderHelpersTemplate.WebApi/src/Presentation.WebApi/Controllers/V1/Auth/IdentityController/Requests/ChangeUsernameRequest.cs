using System.ComponentModel.DataAnnotations;

namespace Presentation.WebApi.Controllers.V1.Auth.IdentityController.Requests;

/// <summary>
/// Request to change the current user's username.
/// </summary>
public sealed record ChangeUsernameRequest
{
    /// <summary>
    /// The new username.
    /// </summary>
    [Required]
    [MinLength(3)]
    [MaxLength(50)]
    public required string Username { get; init; }
}

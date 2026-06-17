using System.ComponentModel.DataAnnotations;

namespace Presentation.WebApi.Controllers.V1.Iam.RolesController.Requests;

/// <summary>
/// Request to remove a role from a user.
/// </summary>
public sealed class RoleRemovalRequest
{
    /// <summary>
    /// Gets or sets the user ID to remove the role from.
    /// </summary>
    [Required]
    public required Guid UserId { get; init; }

    /// <summary>
    /// Gets or sets the role ID to remove.
    /// </summary>
    [Required]
    public required Guid RoleId { get; init; }
}

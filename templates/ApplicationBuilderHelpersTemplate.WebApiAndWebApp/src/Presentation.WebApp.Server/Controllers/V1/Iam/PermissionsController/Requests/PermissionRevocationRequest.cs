using System.ComponentModel.DataAnnotations;

namespace Presentation.WebApp.Server.Controllers.V1.Iam.PermissionsController.Requests;

/// <summary>
/// Request to revoke a direct permission from a user.
/// </summary>
public sealed class PermissionRevocationRequest
{
    /// <summary>
    /// Gets or sets the user ID to revoke the permission from.
    /// </summary>
    [Required]
    public required Guid UserId { get; init; }

    /// <summary>
    /// Gets or sets the permission identifier to revoke.
    /// </summary>
    [Required]
    public required string PermissionIdentifier { get; init; }
}

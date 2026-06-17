using System.ComponentModel.DataAnnotations;

namespace Presentation.WebApi.Controllers.V1.Iam.PermissionsController.Requests;

/// <summary>
/// Request to grant a direct permission to a user.
/// </summary>
public sealed class PermissionGrantRequest
{
    /// <summary>
    /// Gets or sets the user ID to grant the permission to.
    /// </summary>
    [Required]
    public required Guid UserId { get; init; }

    /// <summary>
    /// Gets or sets the permission identifier to grant.
    /// </summary>
    [Required]
    public required string PermissionIdentifier { get; init; }

    /// <summary>
    /// Gets or sets whether this is an Allow grant (true) or Deny grant (false). Defaults to Allow.
    /// </summary>
    public bool IsAllow { get; init; } = true;

    /// <summary>
    /// Gets or sets an optional description for why the permission was granted.
    /// </summary>
    public string? Description { get; init; }
}

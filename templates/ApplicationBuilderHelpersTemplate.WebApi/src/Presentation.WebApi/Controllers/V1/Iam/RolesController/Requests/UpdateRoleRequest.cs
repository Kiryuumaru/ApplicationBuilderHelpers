using System.ComponentModel.DataAnnotations;

namespace Presentation.WebApi.Controllers.V1.Iam.RolesController.Requests;

/// <summary>
/// Request to update an existing custom role.
/// </summary>
public sealed class UpdateRoleRequest
{
    /// <summary>
    /// Gets or sets the new display name for the role.
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public required string Name { get; init; }

    /// <summary>
    /// Gets or sets the new description for the role.
    /// </summary>
    [StringLength(500)]
    public string? Description { get; init; }

    /// <summary>
    /// Gets or sets the new scope templates for this role.
    /// When provided, replaces all existing scope templates.
    /// </summary>
    public IReadOnlyList<ScopeTemplateRequest>? ScopeTemplates { get; init; }
}

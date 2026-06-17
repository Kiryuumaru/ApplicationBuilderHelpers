using System.ComponentModel.DataAnnotations;

namespace Presentation.WebApi.Controllers.V1.Iam.RolesController.Requests;

/// <summary>
/// Request to create a new custom role.
/// </summary>
public sealed class CreateRoleRequest
{
    /// <summary>
    /// Gets or sets the unique code for the role.
    /// </summary>
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public required string Code { get; init; }

    /// <summary>
    /// Gets or sets the display name for the role.
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public required string Name { get; init; }

    /// <summary>
    /// Gets or sets the description for the role.
    /// </summary>
    [StringLength(500)]
    public string? Description { get; init; }

    /// <summary>
    /// Gets or sets the scope templates for this role.
    /// Each scope template grants or denies permissions.
    /// </summary>
    public IReadOnlyList<ScopeTemplateRequest>? ScopeTemplates { get; init; }
}

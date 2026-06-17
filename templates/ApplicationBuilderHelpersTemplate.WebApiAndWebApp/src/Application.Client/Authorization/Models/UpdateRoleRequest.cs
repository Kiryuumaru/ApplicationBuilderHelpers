namespace Application.Client.Authorization.Models;

/// <summary>
/// Request to update a role.
/// </summary>
public class UpdateRoleRequest
{
    /// <summary>
    /// Gets or sets the role name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the role description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the scope templates (if provided, replaces existing).
    /// </summary>
    public List<ScopeTemplateRequest>? ScopeTemplates { get; set; }
}

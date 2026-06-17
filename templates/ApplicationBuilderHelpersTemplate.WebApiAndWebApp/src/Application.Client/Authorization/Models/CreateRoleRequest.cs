namespace Application.Client.Authorization.Models;

/// <summary>
/// Request to create a new role.
/// </summary>
public class CreateRoleRequest
{
    /// <summary>
    /// Gets or sets the role code (unique identifier).
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the role name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the role description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the scope templates defining role permissions.
    /// </summary>
    public List<ScopeTemplateRequest> ScopeTemplates { get; set; } = new();
}

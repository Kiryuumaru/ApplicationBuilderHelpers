namespace Application.Client.Authorization.Models;

/// <summary>
/// Represents role information from IAM.
/// </summary>
public class IamRole
{
    /// <summary>
    /// Gets or sets the role ID.
    /// </summary>
    public Guid Id { get; set; }

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
    /// Gets or sets whether this is a system role (cannot be modified/deleted).
    /// </summary>
    public bool IsSystemRole { get; set; }

    /// <summary>
    /// Gets or sets the scope templates defining role permissions.
    /// </summary>
    public List<ScopeTemplateInfo> ScopeTemplates { get; set; } = new();
}

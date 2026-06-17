namespace Application.Client.Authorization.Models;

/// <summary>
/// Represents a scope template within a role.
/// </summary>
public class ScopeTemplateInfo
{
    /// <summary>
    /// Gets or sets the permission identifier.
    /// </summary>
    public string PermissionIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this is an allow (true) or deny (false) grant.
    /// </summary>
    public bool IsAllow { get; set; }

    /// <summary>
    /// Gets or sets the parameter template values.
    /// </summary>
    public Dictionary<string, string> ParameterTemplates { get; set; } = new();
}

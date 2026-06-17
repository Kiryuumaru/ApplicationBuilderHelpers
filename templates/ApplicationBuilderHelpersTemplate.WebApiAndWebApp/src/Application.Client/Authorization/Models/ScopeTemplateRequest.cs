namespace Application.Client.Authorization.Models;

/// <summary>
/// Request to define a scope template.
/// </summary>
public class ScopeTemplateRequest
{
    /// <summary>
    /// Gets or sets the permission identifier.
    /// </summary>
    public string PermissionIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this is an allow (true) or deny (false) grant.
    /// </summary>
    public bool IsAllow { get; set; } = true;

    /// <summary>
    /// Gets or sets the parameter template values.
    /// </summary>
    public Dictionary<string, string> ParameterTemplates { get; set; } = new();
}

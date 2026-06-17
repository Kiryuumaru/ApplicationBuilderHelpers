namespace Application.Client.Authorization.Models;

/// <summary>
/// Represents permission information from IAM.
/// </summary>
public class IamPermission
{
    /// <summary>
    /// Gets or sets the permission path/identifier.
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the permission identifier.
    /// </summary>
    public string Identifier { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the permission description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the parameter names for this permission.
    /// </summary>
    public List<string> Parameters { get; set; } = new();

    /// <summary>
    /// Gets or sets child permissions in the hierarchy.
    /// </summary>
    public List<IamPermission> Children { get; set; } = new();
}

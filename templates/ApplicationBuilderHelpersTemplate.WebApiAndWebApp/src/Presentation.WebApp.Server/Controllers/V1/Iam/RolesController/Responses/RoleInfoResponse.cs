namespace Presentation.WebApp.Server.Controllers.V1.Iam.RolesController.Responses;

/// <summary>
/// Response containing information about a role.
/// </summary>
public sealed class RoleInfoResponse
{
    /// <summary>
    /// Gets or sets the role's unique identifier.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets or sets the role code.
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Gets or sets the role name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or sets the role description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets or sets whether this is a system-defined role.
    /// </summary>
    public bool IsSystemRole { get; init; }

    /// <summary>
    /// Gets or sets the parameters required when assigning this role.
    /// </summary>
    public required IReadOnlyList<string> Parameters { get; init; }

    /// <summary>
    /// Gets or sets the scope templates for this role.
    /// </summary>
    public IReadOnlyList<ScopeTemplateResponse>? ScopeTemplates { get; init; }
}

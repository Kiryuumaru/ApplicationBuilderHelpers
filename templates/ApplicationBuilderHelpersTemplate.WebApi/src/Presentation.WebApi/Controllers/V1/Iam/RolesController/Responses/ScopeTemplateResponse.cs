namespace Presentation.WebApi.Controllers.V1.Iam.RolesController.Responses;

/// <summary>
/// Represents a scope template in a role response.
/// </summary>
public sealed class ScopeTemplateResponse
{
    /// <summary>
    /// Gets or sets whether this is an allow or deny directive.
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Gets or sets the permission path.
    /// </summary>
    public required string PermissionPath { get; init; }

    /// <summary>
    /// Gets or sets the parameter templates.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Parameters { get; init; }
}

using System.ComponentModel.DataAnnotations;

namespace Presentation.WebApp.Server.Controllers.V1.Iam.RolesController.Requests;

/// <summary>
/// Represents a scope template for granting or denying permissions.
/// </summary>
public sealed class ScopeTemplateRequest
{
    /// <summary>
    /// Gets or sets whether this is an allow or deny directive.
    /// </summary>
    [Required]
    public required string Type { get; init; }

    /// <summary>
    /// Gets or sets the permission path (e.g., "api:iam:users:read").
    /// </summary>
    [Required]
    public required string PermissionPath { get; init; }

    /// <summary>
    /// Gets or sets optional parameter templates.
    /// Parameter values can contain placeholders like {roleUserId}.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Parameters { get; init; }
}

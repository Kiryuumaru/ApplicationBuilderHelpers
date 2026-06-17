namespace Presentation.WebApi.Controllers.V1.Iam.PermissionsController.Responses;

/// <summary>
/// Response containing information about a permission.
/// </summary>
public sealed class PermissionInfoResponse
{
    /// <summary>
    /// Gets or sets the permission path (e.g., "api:iam:users:list").
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// Gets or sets the permission identifier (last segment of path).
    /// </summary>
    public required string Identifier { get; init; }

    /// <summary>
    /// Gets or sets the permission description.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Gets or sets the parameters this permission accepts.
    /// </summary>
    public required IReadOnlyList<string> Parameters { get; init; }

    /// <summary>
    /// Gets or sets whether this is a read operation.
    /// </summary>
    public bool IsRead { get; init; }

    /// <summary>
    /// Gets or sets whether this is a write operation.
    /// </summary>
    public bool IsWrite { get; init; }

    /// <summary>
    /// Gets or sets the child permissions.
    /// </summary>
    public IReadOnlyList<PermissionInfoResponse>? Children { get; init; }
}

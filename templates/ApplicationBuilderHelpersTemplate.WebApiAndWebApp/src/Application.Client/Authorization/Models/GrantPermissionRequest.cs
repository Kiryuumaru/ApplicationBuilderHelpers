namespace Application.Client.Authorization.Models;

/// <summary>
/// Request to grant a direct permission to a user.
/// </summary>
public class GrantPermissionRequest
{
    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the permission identifier.
    /// </summary>
    public string PermissionIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this is an allow (true) or deny (false) grant.
    /// </summary>
    public bool IsAllow { get; set; } = true;

    /// <summary>
    /// Gets or sets a description for the grant.
    /// </summary>
    public string? Description { get; set; }
}

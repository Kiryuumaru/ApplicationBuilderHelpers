namespace Application.Client.Authorization.Models;

/// <summary>
/// Request to revoke a direct permission from a user.
/// </summary>
public class RevokePermissionRequest
{
    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the permission identifier.
    /// </summary>
    public string PermissionIdentifier { get; set; } = string.Empty;
}

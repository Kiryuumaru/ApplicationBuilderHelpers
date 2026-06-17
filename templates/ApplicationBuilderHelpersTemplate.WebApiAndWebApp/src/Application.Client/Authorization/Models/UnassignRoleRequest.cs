namespace Application.Client.Authorization.Models;

/// <summary>
/// Request to unassign a role from a user.
/// </summary>
public class UnassignRoleRequest
{
    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the role ID to unassign.
    /// </summary>
    public Guid RoleId { get; set; }
}

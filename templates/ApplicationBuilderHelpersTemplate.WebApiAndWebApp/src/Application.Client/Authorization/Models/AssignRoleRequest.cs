namespace Application.Client.Authorization.Models;

/// <summary>
/// Request to assign a role to a user.
/// </summary>
public class AssignRoleRequest
{
    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the role code to assign.
    /// </summary>
    public string RoleCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets parameter values for scope template placeholders.
    /// </summary>
    public Dictionary<string, string>? ParameterValues { get; set; }
}

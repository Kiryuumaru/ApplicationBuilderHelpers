namespace Application.Client.Identity.Models;

/// <summary>
/// Response containing user's effective permissions.
/// </summary>
public class UserPermissions
{
    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the effective permission identifiers.
    /// </summary>
    public List<string> Permissions { get; set; } = new();
}

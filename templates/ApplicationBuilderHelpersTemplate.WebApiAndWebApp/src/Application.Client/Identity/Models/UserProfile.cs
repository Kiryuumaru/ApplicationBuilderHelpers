namespace Application.Client.Identity.Models;

/// <summary>
/// Represents user profile information from the /me endpoint.
/// </summary>
public class UserProfile
{
    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the username.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the email.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets the phone number.
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Gets or sets whether 2FA is enabled.
    /// </summary>
    public bool TwoFactorEnabled { get; set; }

    /// <summary>
    /// Gets or sets the user's role IDs.
    /// </summary>
    public List<Guid> RoleIds { get; set; } = new();

    /// <summary>
    /// Gets or sets the user's role codes.
    /// </summary>
    public List<string> Roles { get; set; } = new();
}

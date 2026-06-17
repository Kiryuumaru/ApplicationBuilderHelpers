namespace Application.Client.Identity.Models;

/// <summary>
/// Represents user information from IAM.
/// </summary>
public class IamUser
{
    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the username.
    /// </summary>
    public string? Username { get; set; }

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
    /// Gets or sets whether lockout is enabled for this user.
    /// </summary>
    public bool LockoutEnabled { get; set; }

    /// <summary>
    /// Gets or sets when the lockout ends.
    /// </summary>
    public DateTimeOffset? LockoutEnd { get; set; }

    /// <summary>
    /// Gets or sets the user's role IDs.
    /// </summary>
    public List<Guid> RoleIds { get; set; } = new();

    /// <summary>
    /// Gets whether the user is currently locked out.
    /// </summary>
    public bool IsLockedOut => LockoutEnd.HasValue && LockoutEnd.Value > DateTimeOffset.UtcNow;
}

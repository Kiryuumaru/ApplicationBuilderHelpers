namespace Application.Client.Identity.Models;

/// <summary>
/// Request to update a user.
/// </summary>
public class UpdateUserRequest
{
    /// <summary>
    /// Gets or sets the email.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets the phone number.
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Gets or sets whether lockout is enabled.
    /// </summary>
    public bool? LockoutEnabled { get; set; }
}

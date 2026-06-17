namespace Application.Identity.Models;

/// <summary>
/// Request to update user profile information.
/// </summary>
public sealed class UserUpdateRequest
{
    /// <summary>
    /// Gets or sets the new email address.
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    /// Gets or sets the new phone number.
    /// </summary>
    public string? PhoneNumber { get; init; }

    /// <summary>
    /// Gets or sets whether to enable or disable lockout.
    /// </summary>
    public bool? LockoutEnabled { get; init; }
}

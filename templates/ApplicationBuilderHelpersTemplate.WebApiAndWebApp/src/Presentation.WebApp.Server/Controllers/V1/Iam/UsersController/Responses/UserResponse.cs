namespace Presentation.WebApp.Server.Controllers.V1.Iam.UsersController.Responses;

/// <summary>
/// Response containing a user's details.
/// </summary>
public sealed class UserResponse
{
    /// <summary>
    /// Gets or sets the user's unique identifier.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets or sets the username. Null for anonymous users.
    /// </summary>
    public string? Username { get; init; }

    /// <summary>
    /// Gets or sets the email address.
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    /// Gets or sets whether the email has been confirmed.
    /// </summary>
    public bool EmailConfirmed { get; init; }

    /// <summary>
    /// Gets or sets the phone number.
    /// </summary>
    public string? PhoneNumber { get; init; }

    /// <summary>
    /// Gets or sets whether the phone number has been confirmed.
    /// </summary>
    public bool PhoneNumberConfirmed { get; init; }

    /// <summary>
    /// Gets or sets whether two-factor authentication is enabled.
    /// </summary>
    public bool TwoFactorEnabled { get; init; }

    /// <summary>
    /// Gets or sets whether lockout is enabled for this user.
    /// </summary>
    public bool LockoutEnabled { get; init; }

    /// <summary>
    /// Gets or sets when the lockout ends (if locked out).
    /// </summary>
    public DateTimeOffset? LockoutEnd { get; init; }

    /// <summary>
    /// Gets or sets the current access failed count.
    /// </summary>
    public int AccessFailedCount { get; init; }

    /// <summary>
    /// Gets or sets when the user was created.
    /// </summary>
    public DateTimeOffset Created { get; init; }

    /// <summary>
    /// Gets or sets the user's assigned role IDs.
    /// </summary>
    public required IReadOnlyCollection<Guid> RoleIds { get; init; }
}

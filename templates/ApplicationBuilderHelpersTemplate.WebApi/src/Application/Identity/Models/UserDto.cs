namespace Application.Identity.Models;

/// <summary>
/// Read-only representation of a User for public consumption.
/// Prevents consumers from modifying domain entities directly.
/// </summary>
public sealed record UserDto
{
    /// <summary>
    /// The user's unique identifier.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// The username (null for anonymous users).
    /// </summary>
    public string? Username { get; init; }

    /// <summary>
    /// The user's email address.
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    /// Whether the email has been confirmed.
    /// </summary>
    public required bool EmailConfirmed { get; init; }

    /// <summary>
    /// The user's phone number.
    /// </summary>
    public string? PhoneNumber { get; init; }

    /// <summary>
    /// Whether the phone number has been confirmed.
    /// </summary>
    public required bool PhoneNumberConfirmed { get; init; }

    /// <summary>
    /// Whether this is an anonymous (guest) user.
    /// </summary>
    public required bool IsAnonymous { get; init; }

    /// <summary>
    /// When the anonymous user was upgraded to a full account (null if created as full account).
    /// </summary>
    public DateTimeOffset? LinkedAt { get; init; }

    /// <summary>
    /// Whether the user has a password set.
    /// </summary>
    public required bool HasPassword { get; init; }

    /// <summary>
    /// Whether two-factor authentication is enabled.
    /// </summary>
    public required bool TwoFactorEnabled { get; init; }

    /// <summary>
    /// Whether lockout is enabled for this user.
    /// </summary>
    public required bool LockoutEnabled { get; init; }

    /// <summary>
    /// When the lockout ends (null if not locked out).
    /// </summary>
    public DateTimeOffset? LockoutEnd { get; init; }

    /// <summary>
    /// The current count of consecutive failed access attempts.
    /// </summary>
    public required int AccessFailedCount { get; init; }

    /// <summary>
    /// When the user was created.
    /// </summary>
    public required DateTimeOffset Created { get; init; }

    /// <summary>
    /// The role IDs assigned to this user.
    /// </summary>
    public required IReadOnlyCollection<Guid> RoleIds { get; init; }

    /// <summary>
    /// The role codes assigned to this user.
    /// </summary>
    public required IReadOnlyCollection<string> Roles { get; init; }

    /// <summary>
    /// External logins linked to this account.
    /// </summary>
    public required IReadOnlyCollection<ExternalLoginDto> ExternalLogins { get; init; }
}

namespace Domain.Identity.Models;

/// <summary>
/// Data transfer object for hydrating a User entity from persistence.
/// </summary>
public sealed record UserHydrationData
{
    public required Guid Id { get; init; }
    public Guid? RevId { get; init; }
    public string? UserName { get; init; }
    public string? NormalizedUserName { get; init; }
    public string? Email { get; init; }
    public string? NormalizedEmail { get; init; }
    public bool EmailConfirmed { get; init; }
    public string? PasswordHash { get; init; }
    public string? SecurityStamp { get; init; }
    public string? PhoneNumber { get; init; }
    public bool PhoneNumberConfirmed { get; init; }
    public bool TwoFactorEnabled { get; init; }
    public string? AuthenticatorKey { get; init; }
    public string? RecoveryCodes { get; init; }
    public DateTimeOffset? LockoutEnd { get; init; }
    public bool LockoutEnabled { get; init; }
    public int AccessFailedCount { get; init; }
    public bool IsAnonymous { get; init; }
    public DateTimeOffset? LinkedAt { get; init; }
}

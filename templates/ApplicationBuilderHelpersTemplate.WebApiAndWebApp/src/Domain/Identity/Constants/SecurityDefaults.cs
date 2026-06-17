namespace Domain.Identity.Constants;

/// <summary>
/// Default values for identity and security operations.
/// </summary>
public static class SecurityDefaults
{
    /// <summary>
    /// Default duration for account lockout after too many failed login attempts.
    /// </summary>
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);
}

namespace Domain.Identity.Constants;

/// <summary>
/// Constants for account lockout policy.
/// </summary>
public static class AccountLockout
{
    /// <summary>
    /// Default duration for which a locked account remains inaccessible.
    /// </summary>
    public static readonly TimeSpan DefaultDuration = TimeSpan.FromMinutes(15);
}

namespace Domain.Identity.Enums;

/// <summary>
/// Represents the account status of a user.
/// </summary>
public enum UserStatus
{
    PendingActivation,
    Active,
    Locked,
    Suspended,
    Deactivated
}

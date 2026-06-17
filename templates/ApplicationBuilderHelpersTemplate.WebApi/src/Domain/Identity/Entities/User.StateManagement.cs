using Domain.Identity.Enums;
using Domain.Shared.Exceptions;

namespace Domain.Identity.Entities;

public sealed partial class User
{
    public void UpgradeFromAnonymous(string userName)
    {
        if (!IsAnonymous)
        {
            throw new ValidationException("User is not anonymous");
        }

        if (string.IsNullOrWhiteSpace(userName))
        {
            throw new ArgumentException("UserName cannot be empty when upgrading from anonymous", nameof(userName));
        }

        UserName = userName;
        NormalizedUserName = userName.ToUpperInvariant();
        IsAnonymous = false;
        LinkedAt = DateTimeOffset.UtcNow;
        MarkAsModified();
    }

    public void UpgradeFromAnonymousWithPasskey()
    {
        if (!IsAnonymous)
        {
            throw new ValidationException("User is not anonymous");
        }

        IsAnonymous = false;
        LinkedAt = DateTimeOffset.UtcNow;
        MarkAsModified();
    }

    public void Activate()
    {
        EnsureNotDeactivated();
        Status = UserStatus.Active;
        LockoutEnd = null;
        MarkAsModified();
    }

    public void Suspend(string? reason = null)
    {
        EnsureNotDeactivated();
        Status = UserStatus.Suspended;
        LockoutEnd = null;
        MarkAsModified();
    }

    public void Deactivate()
    {
        Status = UserStatus.Deactivated;
        LockoutEnd = null;
        MarkAsModified();
    }

    public void Unlock()
    {
        if (Status == UserStatus.Locked)
        {
            Status = UserStatus.Active;
            LockoutEnd = null;
            AccessFailedCount = 0;
            MarkAsModified();
        }
    }

    public void MarkEmailVerified()
    {
        if (string.IsNullOrWhiteSpace(Email))
        {
            throw new DomainException("Cannot verify email when email is missing.");
        }
        EmailConfirmed = true;
        MarkAsModified();
    }

    public void ClearEmail()
    {
        Email = null;
        NormalizedEmail = null;
        EmailConfirmed = false;
        MarkAsModified();
    }

    public void SetEmail(string? email, bool markVerified)
    {
        SetEmail(email);
        // Only mark as verified if we have an actual email AND markVerified is true
        EmailConfirmed = markVerified && !string.IsNullOrWhiteSpace(email);
    }

    private void EnsureNotDeactivated()
    {
        if (Status == UserStatus.Deactivated)
        {
            throw new DomainException("Cannot change state on a deactivated user.");
        }
    }
}

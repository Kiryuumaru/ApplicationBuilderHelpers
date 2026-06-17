using Domain.Authorization.ValueObjects;
using Domain.Identity.Constants;
using Domain.Identity.Enums;
using Domain.Identity.Models;
using Domain.Identity.ValueObjects;
using Domain.Shared.Exceptions;

namespace Domain.Identity.Entities;

public sealed partial class User
{
    public void RecordSuccessfulLogin(DateTimeOffset timestamp)
    {
        LastLoginAt = timestamp;
        AccessFailedCount = 0;
        LockoutEnd = null;
        if (Status == UserStatus.PendingActivation)
        {
            Status = UserStatus.Active;
        }

        MarkAsModified();
    }

    public void RecordFailedLogin(DateTimeOffset timestamp, int lockoutThreshold = 5)
    {
        AccessFailedCount++;
        LastFailedLoginAt = timestamp;
        if (LockoutEnabled && AccessFailedCount >= lockoutThreshold)
        {
            Status = UserStatus.Locked;
            LockoutEnd = timestamp + AccountLockout.DefaultDuration;
        }

        MarkAsModified();
    }

    public bool CanAuthenticate(DateTimeOffset timestamp)
    {
        if (Status == UserStatus.Deactivated || Status == UserStatus.Suspended)
        {
            return false;
        }

        if (Status == UserStatus.Locked && LockoutEnd is not null && timestamp < LockoutEnd)
        {
            return false;
        }

        return Status is UserStatus.Active or UserStatus.PendingActivation;
    }

    public UserSession CreateSession(TimeSpan lifetime, DateTimeOffset? issuedAt = null, IEnumerable<string>? permissionIdentifiers = null, IEnumerable<string>? roleCodes = null)
    {
        if (lifetime <= TimeSpan.Zero)
        {
            throw new DomainException("Session lifetime must be positive.");
        }

        var timestamp = issuedAt ?? DateTimeOffset.UtcNow;
        if (!CanAuthenticate(timestamp))
        {
            throw new DomainException("User cannot authenticate in the current state.");
        }

        var permissions = permissionIdentifiers ?? GetPermissionIdentifiers();
        var codes = roleCodes ?? Array.Empty<string>();
        return UserSession.CreateLegacy(Id, UserName, permissions, timestamp, timestamp + lifetime, codes);
    }

    public UserSession CreateScopedSession(TimeSpan lifetime, IEnumerable<ScopeDirective> scope, DateTimeOffset? issuedAt = null, IEnumerable<string>? roleCodes = null)
    {
        if (lifetime <= TimeSpan.Zero)
        {
            throw new DomainException("Session lifetime must be positive.");
        }

        var timestamp = issuedAt ?? DateTimeOffset.UtcNow;
        if (!CanAuthenticate(timestamp))
        {
            throw new DomainException("User cannot authenticate in the current state.");
        }

        var codes = roleCodes ?? Array.Empty<string>();
        return UserSession.Create(Id, UserName, scope, timestamp, timestamp + lifetime, codes);
    }
}

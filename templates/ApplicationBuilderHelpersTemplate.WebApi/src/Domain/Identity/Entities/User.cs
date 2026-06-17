using Domain.Identity.Enums;
using Domain.Identity.Models;
using Domain.Identity.ValueObjects;
using Domain.Shared.Models;

namespace Domain.Identity.Entities;

/// <summary>
/// Represents a user account in the system.
/// </summary>
public sealed partial class User : AggregateRoot
{
    private readonly HashSet<UserPermissionGrant> _permissionGrants = new();
    private readonly HashSet<UserRoleAssignment> _roleAssignments = new();
    private readonly Dictionary<string, UserIdentityLink> _identityLinks = new(StringComparer.Ordinal);

    public string? UserName { get; private set; }
    public string? NormalizedUserName { get; private set; }
    public string? Email { get; private set; }
    public string? NormalizedEmail { get; private set; }
    public bool EmailConfirmed { get; private set; }
    public string? PasswordHash { get; private set; }
    public string? SecurityStamp { get; private set; }
    public string? PhoneNumber { get; private set; }
    public bool PhoneNumberConfirmed { get; private set; }
    public bool TwoFactorEnabled { get; private set; }
    public string? AuthenticatorKey { get; private set; }
    public string? RecoveryCodes { get; private set; }
    public DateTimeOffset? LockoutEnd { get; private set; }
    public bool LockoutEnabled { get; private set; }
    public int AccessFailedCount { get; private set; }

    // Anonymous/Guest support
    public bool IsAnonymous { get; private set; }
    public DateTimeOffset? LinkedAt { get; private set; }

    // Legacy/Custom fields
    public UserStatus Status { get; private set; }
    public DateTimeOffset? LastLoginAt { get; private set; }
    public DateTimeOffset? LastFailedLoginAt { get; private set; }

    public IReadOnlyCollection<UserPermissionGrant> PermissionGrants => _permissionGrants.ToList().AsReadOnly();
    public IReadOnlyCollection<Guid> RoleIds => [.. _roleAssignments
        .Select(static assignment => assignment.RoleId)
        .Distinct()];
    public IReadOnlyCollection<UserRoleAssignment> RoleAssignments => _roleAssignments.ToList().AsReadOnly();
    public IReadOnlyCollection<UserIdentityLink> IdentityLinks => _identityLinks.Values.ToList().AsReadOnly();

    private User(Guid id, string? userName, string? email, bool isAnonymous = false) : base(id)
    {
        UserName = userName;
        NormalizedUserName = userName?.ToUpperInvariant();
        Email = email;
        NormalizedEmail = email?.ToUpperInvariant();
        SecurityStamp = Guid.NewGuid().ToString();
        Status = UserStatus.PendingActivation;
        LockoutEnabled = true;
        IsAnonymous = isAnonymous;
    }

    public static User Register(string userName, string? email = null)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            throw new ArgumentException("UserName cannot be empty", nameof(userName));
        }
        return new(Guid.NewGuid(), userName, email);
    }

    public static User RegisterAnonymous()
        => new(Guid.NewGuid(), null, null, isAnonymous: true);

    public static User Hydrate(UserHydrationData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        
        return Hydrate(
            data.Id,
            data.RevId,
            data.UserName,
            data.NormalizedUserName,
            data.Email,
            data.NormalizedEmail,
            data.EmailConfirmed,
            data.PasswordHash,
            data.SecurityStamp,
            data.PhoneNumber,
            data.PhoneNumberConfirmed,
            data.TwoFactorEnabled,
            data.AuthenticatorKey,
            data.RecoveryCodes,
            data.LockoutEnd,
            data.LockoutEnabled,
            data.AccessFailedCount,
            data.IsAnonymous,
            data.LinkedAt);
    }

    public static User Hydrate(
        Guid id,
        Guid? revId,
        string? userName,
        string? normalizedUserName,
        string? email,
        string? normalizedEmail,
        bool emailConfirmed,
        string? passwordHash,
        string? securityStamp,
        string? phoneNumber,
        bool phoneNumberConfirmed,
        bool twoFactorEnabled,
        string? authenticatorKey,
        string? recoveryCodes,
        DateTimeOffset? lockoutEnd,
        bool lockoutEnabled,
        int accessFailedCount,
        bool isAnonymous,
        DateTimeOffset? linkedAt)
    {
        var user = new User(id, userName, email, isAnonymous)
        {
            NormalizedUserName = normalizedUserName,
            NormalizedEmail = normalizedEmail,
            EmailConfirmed = emailConfirmed,
            PasswordHash = passwordHash,
            SecurityStamp = securityStamp,
            PhoneNumber = phoneNumber,
            PhoneNumberConfirmed = phoneNumberConfirmed,
            TwoFactorEnabled = twoFactorEnabled,
            AuthenticatorKey = authenticatorKey,
            RecoveryCodes = recoveryCodes,
            LockoutEnd = lockoutEnd,
            LockoutEnabled = lockoutEnabled,
            AccessFailedCount = accessFailedCount,
            LinkedAt = linkedAt
        };
        if (revId.HasValue)
        {
            user.RevId = revId.Value;
        }
        return user;
    }

    public static User RegisterExternal(string userName, string provider, string subject, string? providerEmail = null, string? displayName = null, string? email = null)
    {
        var user = new User(Guid.NewGuid(), userName, email);
        user.LinkIdentity(provider, subject, providerEmail, displayName);
        return user;
    }
}

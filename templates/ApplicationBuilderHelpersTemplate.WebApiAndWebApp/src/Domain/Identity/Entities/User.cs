using System;
using System.Collections.Generic;
using System.Linq;
using Domain.Authorization.Entities;
using Domain.Authorization.Models;
using Domain.Authorization.ValueObjects;
using Domain.Identity.Constants;
using Domain.Identity.Enums;
using Domain.Identity.Entities;
using Domain.Identity.Models;
using Domain.Identity.ValueObjects;
using Domain.Shared.Exceptions;
using Domain.Shared.Models;

namespace Domain.Identity.Entities;

public class User : AggregateRoot
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

    protected User(Guid id, string? userName, string? email, bool isAnonymous = false) : base(id)
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

    public void SetUserName(string userName)
    {
        if (string.IsNullOrWhiteSpace(userName)) throw new ArgumentException("UserName cannot be empty", nameof(userName));
        UserName = userName;
        MarkAsModified();
    }

    public void SetNormalizedUserName(string normalizedUserName)
    {
        if (string.IsNullOrWhiteSpace(normalizedUserName)) throw new ArgumentException("NormalizedUserName cannot be empty", nameof(normalizedUserName));
        NormalizedUserName = normalizedUserName;
        MarkAsModified();
    }

    public void SetEmail(string? email)
    {
        Email = email?.ToLowerInvariant();
        NormalizedEmail = email?.ToUpperInvariant();
        MarkAsModified();
    }

    public void SetNormalizedEmail(string? normalizedEmail)
    {
        NormalizedEmail = normalizedEmail;
        MarkAsModified();
    }

    public void SetEmailConfirmed(bool confirmed)
    {
        EmailConfirmed = confirmed;
        MarkAsModified();
    }

    public void SetPasswordHash(string? passwordHash)
    {
        PasswordHash = passwordHash;
        MarkAsModified();
    }

    public void SetSecurityStamp(string securityStamp)
    {
        SecurityStamp = securityStamp;
        MarkAsModified();
    }

    public void SetPhoneNumber(string? phoneNumber)
    {
        PhoneNumber = phoneNumber;
        MarkAsModified();
    }

    public void SetPhoneNumberConfirmed(bool confirmed)
    {
        PhoneNumberConfirmed = confirmed;
        MarkAsModified();
    }

    public void SetTwoFactorEnabled(bool enabled)
    {
        TwoFactorEnabled = enabled;
        MarkAsModified();
    }

    public void SetAuthenticatorKey(string? authenticatorKey)
    {
        AuthenticatorKey = authenticatorKey;
        MarkAsModified();
    }

    public void SetRecoveryCodes(string? recoveryCodes)
    {
        RecoveryCodes = recoveryCodes;
        MarkAsModified();
    }

    public void SetLockoutEnd(DateTimeOffset? lockoutEnd)
    {
        LockoutEnd = lockoutEnd;
        MarkAsModified();
    }

    public void SetLockoutEnabled(bool enabled)
    {
        LockoutEnabled = enabled;
        MarkAsModified();
    }

    public void SetAccessFailedCount(int count)
    {
        AccessFailedCount = count;
        MarkAsModified();
    }

    public void IncrementAccessFailedCount()
    {
        AccessFailedCount++;
        MarkAsModified();
    }

    public void ResetAccessFailedCount()
    {
        AccessFailedCount = 0;
        MarkAsModified();
    }

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

    public void GrantPermission(UserPermissionGrant grant)
    {
        ArgumentNullException.ThrowIfNull(grant);
        if (_permissionGrants.Add(grant))
        {
            MarkAsModified();
        }
    }

    public bool RevokePermission(string permissionIdentifier)
    {
        ArgumentException.ThrowIfNullOrEmpty(permissionIdentifier);
        var canonical = permissionIdentifier.Trim();
        var removed = _permissionGrants.RemoveWhere(grant => string.Equals(grant.Identifier, canonical, StringComparison.Ordinal)) > 0;
        if (removed)
        {
            MarkAsModified();
        }

        return removed;
    }

    public IReadOnlyCollection<string> GetPermissionIdentifiers()
        => [.. _permissionGrants
            .Select(grant => grant.Identifier)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(identifier => identifier, StringComparer.Ordinal)];

    public IReadOnlyCollection<ScopeDirective> BuildEffectiveScopeDirectives(IEnumerable<UserRoleResolution>? roleResolutions)
    {
        var directives = new List<ScopeDirective>();

        // Add direct permission grants as scope directives (respecting Allow/Deny type)
        foreach (var grant in _permissionGrants)
        {
            directives.Add(grant.ToScopeDirective());
        }

        // Add directives from roles
        if (roleResolutions is not null)
        {
            foreach (var resolution in roleResolutions)
            {
                if (resolution?.Role is null)
                {
                    continue;
                }

                foreach (var directive in resolution.Role.ExpandScope(resolution.ParameterValues))
                {
                    directives.Add(directive);
                }
            }
        }

        return directives;
    }

    public IReadOnlyCollection<string> BuildEffectivePermissions(IEnumerable<UserRoleResolution>? roleResolutions)
    {
        var identifiers = new HashSet<string>(StringComparer.Ordinal);
        foreach (var permission in GetPermissionIdentifiers())
        {
            identifiers.Add(permission);
        }

        if (roleResolutions is not null)
        {
            foreach (var resolution in roleResolutions)
            {
                if (resolution?.Role is null)
                {
                    continue;
                }

                foreach (var directive in resolution.Role.ExpandScope(resolution.ParameterValues))
                {
                    var identifier = directive.ToPermissionIdentifier();
                    if (identifier is not null)
                    {
                        identifiers.Add(identifier);
                    }
                }
            }
        }

        return [.. identifiers.OrderBy(static id => id, StringComparer.Ordinal)];
    }

    public bool AssignRole(Guid roleId, IReadOnlyDictionary<string, string?>? parameterValues = null)
    {
        var assignment = UserRoleAssignment.Create(roleId, parameterValues);
        if (_roleAssignments.Add(assignment))
        {
            MarkAsModified();
            return true;
        }

        return false;
    }

    public bool RemoveRole(Guid roleId)
    {
        var removed = _roleAssignments.RemoveWhere(assignment => assignment.RoleId == roleId) > 0;
        if (removed)
        {
            MarkAsModified();
            return true;
        }

        return false;
    }

    public void ClearRoles()
    {
        if (_roleAssignments.Count > 0)
        {
            _roleAssignments.Clear();
            MarkAsModified();
        }
    }

    public UserIdentityLink LinkIdentity(
        string provider,
        string providerSubject,
        string? email = null,
        string? displayName = null,
        DateTimeOffset? linkedAt = null)
        => LinkIdentityInternal(provider, providerSubject, email, displayName, linkedAt ?? DateTimeOffset.UtcNow, markModified: true);

    public bool UnlinkIdentity(string provider, string subject)
    {
        var key = BuildIdentityKey(UserIdentityLink.NormalizeProvider(provider), UserIdentityLink.NormalizeSubject(subject));
        if (_identityLinks.ContainsKey(key))
        {
            _identityLinks.Remove(key);
            MarkAsModified();
            return true;
        }
        return false;
    }

    public bool HasIdentity(string provider, string providerSubject)
    {
        var key = BuildIdentityKey(UserIdentityLink.NormalizeProvider(provider), UserIdentityLink.NormalizeSubject(providerSubject));
        return _identityLinks.ContainsKey(key);
    }

    public UserIdentityLink? GetIdentity(string provider, string providerSubject)
    {
        var key = BuildIdentityKey(UserIdentityLink.NormalizeProvider(provider), UserIdentityLink.NormalizeSubject(providerSubject));
        return _identityLinks.TryGetValue(key, out var identity) ? identity : null;
    }

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
            LockoutEnd = timestamp + SecurityDefaults.LockoutDuration;
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

    private UserIdentityLink LinkIdentityInternal(
        string provider,
        string providerSubject,
        string? email,
        string? displayName,
        DateTimeOffset linkedAt,
        bool markModified)
    {
        var identity = UserIdentityLink.Create(provider, providerSubject, email, displayName, linkedAt);
        var key = BuildIdentityKey(identity.Provider, identity.Subject);
        _identityLinks[key] = identity;
        if (markModified)
        {
            MarkAsModified();
        }

        return identity;
    }

    private static string BuildIdentityKey(string provider, string subject)
        => $"{provider}::{subject}";

    private void EnsureNotDeactivated()
    {
        if (Status == UserStatus.Deactivated)
        {
            throw new DomainException("Cannot change state on a deactivated user.");
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
}

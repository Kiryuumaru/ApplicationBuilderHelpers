namespace Domain.Identity.Entities;

public sealed partial class User
{
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
}

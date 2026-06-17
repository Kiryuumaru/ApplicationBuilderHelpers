using Domain.Identity.ValueObjects;

namespace Domain.Identity.Entities;

public sealed partial class User
{
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
}

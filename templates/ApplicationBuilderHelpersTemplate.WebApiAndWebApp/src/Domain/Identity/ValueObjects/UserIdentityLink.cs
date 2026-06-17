using Domain.Shared.Exceptions;
using Domain.Shared.Models;

namespace Domain.Identity.ValueObjects;

public class UserIdentityLink : ValueObject
{
    public string Provider { get; }
    public string Subject { get; }
    public string? Email { get; }
    public string? DisplayName { get; }
    public DateTimeOffset LinkedAt { get; }

    protected UserIdentityLink(string provider, string subject, string? email, string? displayName, DateTimeOffset linkedAt)
    {
        Provider = NormalizeProvider(provider);
        Subject = NormalizeSubject(subject);
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();
        LinkedAt = linkedAt;
    }

    public static UserIdentityLink Create(
        string provider,
        string subject,
        string? email = null,
        string? displayName = null,
        DateTimeOffset? linkedAt = null)
    {
        var timestamp = linkedAt ?? DateTimeOffset.UtcNow;
        return new UserIdentityLink(provider, subject, email, displayName, timestamp);
    }

    internal static string NormalizeProvider(string provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
        {
            throw new DomainException("Provider cannot be null or empty.");
        }

        return provider.Trim().ToLowerInvariant();
    }

    internal static string NormalizeSubject(string subject)
    {
        if (string.IsNullOrWhiteSpace(subject))
        {
            throw new DomainException("Provider subject cannot be null or empty.");
        }

        return subject.Trim();
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Provider;
        yield return Subject;
    }
}

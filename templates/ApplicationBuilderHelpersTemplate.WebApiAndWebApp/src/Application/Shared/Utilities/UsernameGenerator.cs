namespace Application.Shared.Utilities;

public static class UsernameGenerator
{
    public static string FromOAuth(string? name, string? email, string providerSubject)
    {
        ArgumentException.ThrowIfNullOrEmpty(providerSubject);

        var baseUsername = name?.Replace(" ", "").ToLowerInvariant()
            ?? email?.Split('@')[0].ToLowerInvariant()
            ?? $"user_{providerSubject[..Math.Min(8, providerSubject.Length)]}";

        var suffix = Guid.NewGuid().ToString("N")[..6];
        return $"{baseUsername}_{suffix}";
    }
}

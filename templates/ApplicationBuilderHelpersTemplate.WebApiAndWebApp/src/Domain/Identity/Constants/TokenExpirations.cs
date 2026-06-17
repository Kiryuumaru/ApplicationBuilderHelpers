namespace Domain.Identity.Constants;

public static class TokenExpirations
{
    public static readonly TimeSpan AccessToken = TimeSpan.FromHours(1);

    public static readonly TimeSpan RefreshToken = TimeSpan.FromDays(7);

    public static readonly TimeSpan PasskeyChallenge = TimeSpan.FromMinutes(5);

    public static readonly TimeSpan PasskeySession = TimeSpan.FromHours(24);
}

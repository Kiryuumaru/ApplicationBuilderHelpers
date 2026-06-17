namespace Infrastructure.Identity.Models;

/// <summary>
/// Configuration settings for JWT token generation and validation.
/// </summary>
internal sealed class JwtConfiguration
{
    /// <summary>
    /// Default clock skew tolerance for JWT token validation.
    /// </summary>
    public static readonly TimeSpan DefaultClockSkew = TimeSpan.FromMinutes(5);

    public required string Secret { get; init; }
    public required string Issuer { get; init; }
    public required string Audience { get; init; }
    public required TimeSpan DefaultExpiration { get; init; }
    public required TimeSpan ClockSkew { get; init; }
}

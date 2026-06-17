namespace Infrastructure.Server.Identity.Models;

internal sealed class JwtConfiguration
{
    public required string Secret { get; init; }
    public required string Issuer { get; init; }
    public required string Audience { get; init; }
    public required TimeSpan DefaultExpiration { get; init; }
    public required TimeSpan ClockSkew { get; init; }
}

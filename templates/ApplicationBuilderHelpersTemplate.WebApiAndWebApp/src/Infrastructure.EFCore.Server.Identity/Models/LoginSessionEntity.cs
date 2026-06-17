namespace Infrastructure.EFCore.Server.Identity.Models;

internal sealed class LoginSessionEntity
{
    public required Guid Id { get; set; }
    public required Guid UserId { get; set; }
    public required string RefreshTokenHash { get; set; }
    public string? DeviceName { get; set; }
    public string? UserAgent { get; set; }
    public string? IpAddress { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
    public required DateTimeOffset LastUsedAt { get; set; }
    public required DateTimeOffset ExpiresAt { get; set; }
    public required bool IsRevoked { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
}

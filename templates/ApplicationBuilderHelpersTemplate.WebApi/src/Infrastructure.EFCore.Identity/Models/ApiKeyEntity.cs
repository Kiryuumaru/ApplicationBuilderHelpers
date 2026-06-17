namespace Infrastructure.EFCore.Identity.Models;

internal sealed class ApiKeyEntity
{
    public required Guid Id { get; set; }
    public required Guid UserId { get; set; }
    public required string Name { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset? LastUsedAt { get; set; }
    public required bool IsRevoked { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
}

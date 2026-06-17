using Domain.Shared.Models;

namespace Domain.Identity.Entities;

/// <summary>
/// Represents an API key for user authentication.
/// </summary>
public sealed class ApiKey : Entity
{
    public Guid UserId { get; private set; }

    public string Name { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? ExpiresAt { get; private set; }

    public DateTimeOffset? LastUsedAt { get; private set; }

    public bool IsRevoked { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    private ApiKey(
        Guid id,
        Guid userId,
        string name,
        DateTimeOffset createdAt,
        DateTimeOffset? expiresAt) : base(id)
    {
        UserId = userId;
        Name = name;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
        IsRevoked = false;
    }

    public static ApiKey Create(
        Guid userId,
        string name,
        DateTimeOffset? expiresAt = null)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be empty.", nameof(name));
        }

        return new ApiKey(
            Guid.NewGuid(),
            userId,
            name,
            DateTimeOffset.UtcNow,
            expiresAt);
    }

    public static ApiKey Reconstruct(
        Guid id,
        Guid userId,
        string name,
        DateTimeOffset createdAt,
        DateTimeOffset? expiresAt,
        DateTimeOffset? lastUsedAt,
        bool isRevoked,
        DateTimeOffset? revokedAt)
    {
        return new ApiKey(id, userId, name, createdAt, expiresAt)
        {
            LastUsedAt = lastUsedAt,
            IsRevoked = isRevoked,
            RevokedAt = revokedAt
        };
    }

    public void RecordUsage()
    {
        LastUsedAt = DateTimeOffset.UtcNow;
    }

    public void Revoke()
    {
        if (IsRevoked)
        {
            return;
        }

        IsRevoked = true;
        RevokedAt = DateTimeOffset.UtcNow;
    }
}

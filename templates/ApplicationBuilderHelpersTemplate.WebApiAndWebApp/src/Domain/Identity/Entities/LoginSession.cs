using Domain.Shared.Models;

namespace Domain.Identity.Entities;

/// <summary>
/// Represents a user's login session with refresh token tracking.
/// </summary>
public class LoginSession : Entity
{
    public Guid UserId { get; private set; }

    public string RefreshTokenHash { get; private set; }

    public string? DeviceName { get; private set; }

    public string? UserAgent { get; private set; }

    public string? IpAddress { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset LastUsedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public bool IsRevoked { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    protected LoginSession(
        Guid id,
        Guid userId,
        string refreshTokenHash,
        string? deviceName,
        string? userAgent,
        string? ipAddress,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt) : base(id)
    {
        UserId = userId;
        RefreshTokenHash = refreshTokenHash;
        DeviceName = deviceName;
        UserAgent = userAgent;
        IpAddress = ipAddress;
        CreatedAt = createdAt;
        LastUsedAt = createdAt;
        ExpiresAt = expiresAt;
        IsRevoked = false;
    }

    public static LoginSession Create(
        Guid userId,
        string refreshTokenHash,
        DateTimeOffset expiresAt,
        string? deviceName = null,
        string? userAgent = null,
        string? ipAddress = null)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(refreshTokenHash))
        {
            throw new ArgumentException("Refresh token hash cannot be empty.", nameof(refreshTokenHash));
        }

        return new LoginSession(
            Guid.NewGuid(),
            userId,
            refreshTokenHash,
            deviceName,
            userAgent,
            ipAddress,
            DateTimeOffset.UtcNow,
            expiresAt);
    }

    public static LoginSession Reconstruct(
        Guid id,
        Guid userId,
        string refreshTokenHash,
        string? deviceName,
        string? userAgent,
        string? ipAddress,
        DateTimeOffset createdAt,
        DateTimeOffset lastUsedAt,
        DateTimeOffset expiresAt,
        bool isRevoked,
        DateTimeOffset? revokedAt)
    {
        var session = new LoginSession(
            id,
            userId,
            refreshTokenHash,
            deviceName,
            userAgent,
            ipAddress,
            createdAt,
            expiresAt)
        {
            LastUsedAt = lastUsedAt,
            IsRevoked = isRevoked,
            RevokedAt = revokedAt
        };
        return session;
    }

    public void RotateRefreshToken(string newRefreshTokenHash, DateTimeOffset newExpiresAt)
    {
        if (string.IsNullOrWhiteSpace(newRefreshTokenHash))
        {
            throw new ArgumentException("New refresh token hash cannot be empty.", nameof(newRefreshTokenHash));
        }

        RefreshTokenHash = newRefreshTokenHash;
        LastUsedAt = DateTimeOffset.UtcNow;
        ExpiresAt = newExpiresAt;
    }

    public void Revoke()
    {
        IsRevoked = true;
        RevokedAt = DateTimeOffset.UtcNow;
    }

    public bool IsValid => !IsRevoked && ExpiresAt > DateTimeOffset.UtcNow;
}

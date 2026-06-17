namespace Application.Identity.Models;

/// <summary>
/// Read-only representation of a login session for public consumption.
/// Contains session metadata without exposing sensitive tokens.
/// </summary>
public sealed record SessionDto
{
    /// <summary>
    /// The session's unique identifier.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// The user ID this session belongs to.
    /// </summary>
    public required Guid UserId { get; init; }

    /// <summary>
    /// The device name (if provided during login).
    /// </summary>
    public string? DeviceName { get; init; }

    /// <summary>
    /// The user agent string from the login request.
    /// </summary>
    public string? UserAgent { get; init; }

    /// <summary>
    /// The IP address from the login request.
    /// </summary>
    public string? IpAddress { get; init; }

    /// <summary>
    /// When the session was created.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// When the session was last used (token refresh).
    /// </summary>
    public required DateTimeOffset LastUsedAt { get; init; }

    /// <summary>
    /// Whether the session is currently valid (not revoked).
    /// </summary>
    public required bool IsValid { get; init; }

    /// <summary>
    /// When the session was revoked (if revoked).
    /// </summary>
    public DateTimeOffset? RevokedAt { get; init; }
}

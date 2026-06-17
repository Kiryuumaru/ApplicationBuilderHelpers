namespace Application.Identity.Models;

/// <summary>
/// Read-only representation of a user session for public consumption.
/// </summary>
public sealed record UserSessionDto
{
    /// <summary>
    /// The session's unique identifier.
    /// </summary>
    public required Guid SessionId { get; init; }

    /// <summary>
    /// The user's unique identifier.
    /// </summary>
    public required Guid UserId { get; init; }

    /// <summary>
    /// The username (null for anonymous users).
    /// </summary>
    public string? Username { get; init; }

    /// <summary>
    /// Whether this session belongs to an anonymous user.
    /// </summary>
    public required bool IsAnonymous { get; init; }

    /// <summary>
    /// The access token for API authentication.
    /// </summary>
    public required string AccessToken { get; init; }

    /// <summary>
    /// The refresh token for obtaining new access tokens.
    /// </summary>
    public required string RefreshToken { get; init; }

    /// <summary>
    /// When the session was created.
    /// </summary>
    public required DateTimeOffset IssuedAt { get; init; }

    /// <summary>
    /// When the access token expires.
    /// </summary>
    public required DateTimeOffset ExpiresAt { get; init; }

    /// <summary>
    /// The role codes for this session.
    /// </summary>
    public required IReadOnlyCollection<string> Roles { get; init; }
}

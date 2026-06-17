namespace Application.Server.Identity.Models;

/// <summary>
/// Read-only representation of an API key for public consumption.
/// Contains metadata without exposing the actual JWT.
/// </summary>
public sealed record ApiKeyDto
{
    /// <summary>
    /// The API key's unique identifier.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// The user ID this API key belongs to.
    /// </summary>
    public required Guid UserId { get; init; }

    /// <summary>
    /// User-friendly name for the API key (e.g., "Trading Bot").
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// When the API key was created.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// When the API key expires. Null means never expires.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>
    /// When the API key was last used for authentication.
    /// </summary>
    public DateTimeOffset? LastUsedAt { get; init; }
}

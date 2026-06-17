namespace Application.Identity.Models;

/// <summary>
/// Represents session information returned to the client.
/// </summary>
public sealed record SessionInfo
{
    /// <summary>
    /// Gets the unique identifier for this session.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets the name of the device or browser used to create this session.
    /// </summary>
    public string? DeviceName { get; init; }

    /// <summary>
    /// Gets the user agent string from the request that created this session.
    /// </summary>
    public string? UserAgent { get; init; }

    /// <summary>
    /// Gets the IP address from which this session was created.
    /// </summary>
    public string? IpAddress { get; init; }

    /// <summary>
    /// Gets the timestamp when this session was created.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Gets the timestamp when this session was last used.
    /// </summary>
    public required DateTimeOffset LastUsedAt { get; init; }

    /// <summary>
    /// Gets whether this is the requester's current session.
    /// </summary>
    public required bool IsCurrent { get; init; }
}

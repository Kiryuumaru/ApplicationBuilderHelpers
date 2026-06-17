namespace Presentation.WebApp.Server.Controllers.V1.Auth.SessionsController.Responses;

/// <summary>
/// Response containing information about a single session.
/// </summary>
public sealed record SessionInfoResponse
{
    /// <summary>
    /// Gets or sets the unique identifier for this session.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets or sets the name of the device or browser used to create this session.
    /// </summary>
    public string? DeviceName { get; init; }

    /// <summary>
    /// Gets or sets the user agent string from the request that created this session.
    /// </summary>
    public string? UserAgent { get; init; }

    /// <summary>
    /// Gets or sets the IP address from which this session was created.
    /// </summary>
    public string? IpAddress { get; init; }

    /// <summary>
    /// Gets or sets the timestamp when this session was created.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Gets or sets the timestamp when this session was last used.
    /// </summary>
    public required DateTimeOffset LastUsedAt { get; init; }

    /// <summary>
    /// Gets or sets whether this is the requester's current session.
    /// </summary>
    public required bool IsCurrent { get; init; }
}

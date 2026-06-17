namespace Application.Client.Identity.Models;

/// <summary>
/// Represents information about an active session.
/// </summary>
public class SessionInfo
{
    /// <summary>
    /// Gets or sets the session ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the device name.
    /// </summary>
    public string? DeviceName { get; set; }

    /// <summary>
    /// Gets or sets the user agent string.
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Gets or sets the IP address.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Gets or sets when the session was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets when the session was last used.
    /// </summary>
    public DateTimeOffset? LastUsedAt { get; set; }

    /// <summary>
    /// Gets or sets whether this is the current session.
    /// </summary>
    public bool IsCurrent { get; set; }
}

namespace Application.Client.Identity.Models;

/// <summary>
/// Information about a registered passkey.
/// </summary>
public sealed class PasskeyInfo
{
    /// <summary>
    /// The unique identifier for this passkey.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The user-friendly name of this passkey.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// When this passkey was registered.
    /// </summary>
    public DateTimeOffset RegisteredAt { get; set; }

    /// <summary>
    /// When this passkey was last used for authentication.
    /// </summary>
    public DateTimeOffset? LastUsedAt { get; set; }
}

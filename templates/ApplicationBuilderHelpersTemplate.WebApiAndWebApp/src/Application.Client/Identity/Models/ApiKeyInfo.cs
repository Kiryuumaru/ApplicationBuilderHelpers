namespace Application.Client.Identity.Models;

/// <summary>
/// Represents information about an API key.
/// </summary>
public class ApiKeyInfo
{
    /// <summary>
    /// Gets or sets the API key ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the API key name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the API key was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets when the API key expires (optional).
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets when the API key was last used.
    /// </summary>
    public DateTimeOffset? LastUsedAt { get; set; }
}

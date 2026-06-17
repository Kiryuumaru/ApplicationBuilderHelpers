namespace Application.Client.Identity.Models;

/// <summary>
/// Represents a newly created API key with its secret token.
/// </summary>
public class CreateApiKeyResult
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
    /// Gets or sets the secret API key token (only shown once at creation).
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the API key was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets when the API key expires (optional).
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; set; }
}

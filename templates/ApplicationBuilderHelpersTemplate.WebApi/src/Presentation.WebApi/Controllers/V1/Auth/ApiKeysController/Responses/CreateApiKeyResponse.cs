namespace Presentation.WebApi.Controllers.V1.Auth.ApiKeysController.Responses;

/// <summary>
/// Response when creating a new API key. Contains the JWT which is only shown once.
/// </summary>
public sealed record CreateApiKeyResponse
{
    /// <summary>
    /// Unique identifier for the API key.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// User-friendly name for the API key.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The API key JWT. IMPORTANT: This is only returned once at creation time.
    /// Store it securely - it cannot be retrieved again.
    /// Use in Authorization header: Bearer {key}
    /// </summary>
    public required string Key { get; init; }

    /// <summary>
    /// When the API key was created.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// When the API key expires (null = never).
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; init; }
}

namespace Presentation.WebApp.Server.Controllers.V1.Auth.ApiKeysController.Responses;

/// <summary>
/// Response containing information about an API key (without the JWT secret).
/// </summary>
public sealed record ApiKeyInfoResponse
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
    /// When the API key was created.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// When the API key expires (null = never).
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>
    /// When the API key was last used for authentication.
    /// </summary>
    public DateTimeOffset? LastUsedAt { get; init; }
}

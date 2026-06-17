using System.ComponentModel.DataAnnotations;

namespace Presentation.WebApi.Controllers.V1.Auth.ApiKeysController.Requests;

/// <summary>
/// Request to create a new API key.
/// </summary>
public sealed record CreateApiKeyRequest
{
    /// <summary>
    /// A friendly name for the API key (e.g., "Trading Bot", "CI/CD Pipeline").
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public required string Name { get; init; }

    /// <summary>
    /// Optional expiration date. If null, the key never expires.
    /// Must be in the future if provided.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; init; }
}

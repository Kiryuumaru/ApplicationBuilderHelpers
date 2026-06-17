namespace Presentation.WebApi.Controllers.V1.Auth.IdentityController.Responses;

/// <summary>
/// Information about a linked OAuth provider.
/// </summary>
public sealed record LinkedProviderInfo
{
    /// <summary>
    /// The OAuth provider name (e.g., "google", "github").
    /// </summary>
    public required string Provider { get; init; }

    /// <summary>
    /// Display name from the provider (e.g., user's name or email).
    /// </summary>
    public string? DisplayName { get; init; }

    /// <summary>
    /// Email from the provider (if available).
    /// </summary>
    public string? Email { get; init; }
}

namespace Presentation.WebApp.Server.Controllers.V1.Auth.OAuthController.Responses;

/// <summary>
/// Information about an available OAuth provider.
/// </summary>
public sealed record OAuthProviderResponse
{
    /// <summary>
    /// The provider identifier (e.g., "google", "github").
    /// </summary>
    public required string Provider { get; init; }

    /// <summary>
    /// Display name for the provider.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Whether this provider is currently enabled.
    /// </summary>
    public bool IsEnabled { get; init; }

    /// <summary>
    /// Icon name for the provider.
    /// </summary>
    public string? IconName { get; init; }
}

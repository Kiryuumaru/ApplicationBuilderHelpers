using Domain.Identity.Enums;

namespace Application.Identity.Models;

/// <summary>
/// Configuration for an external OAuth provider.
/// </summary>
public sealed record OAuthProviderConfig
{
    /// <summary>
    /// The provider type.
    /// </summary>
    public required ExternalLoginProvider Provider { get; init; }

    /// <summary>
    /// Display name for the provider (e.g., "Google", "GitHub").
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Whether this provider is currently enabled.
    /// </summary>
    public bool IsEnabled { get; init; }

    /// <summary>
    /// Icon name or URL for the provider.
    /// </summary>
    public string? IconName { get; init; }
}

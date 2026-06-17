using Domain.Identity.Enums;

namespace Application.Identity.Models;

/// <summary>
/// User info retrieved from an OAuth provider after successful authentication.
/// </summary>
public sealed record OAuthUserInfo
{
    /// <summary>
    /// The OAuth provider.
    /// </summary>
    public required ExternalLoginProvider Provider { get; init; }

    /// <summary>
    /// The user's unique identifier at the provider (subject claim).
    /// </summary>
    public required string ProviderSubject { get; init; }

    /// <summary>
    /// The user's email address from the provider (if available).
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    /// Whether the email is verified by the provider.
    /// </summary>
    public bool EmailVerified { get; init; }

    /// <summary>
    /// The user's display name from the provider.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// The user's profile picture URL from the provider.
    /// </summary>
    public string? PictureUrl { get; init; }

    /// <summary>
    /// Raw claims/data from the provider for additional processing.
    /// </summary>
    public IReadOnlyDictionary<string, string>? AdditionalClaims { get; init; }
}

namespace Presentation.WebApi.Controllers.V1.Auth.IdentityController.Responses;

/// <summary>
/// Response containing the user's linked identities.
/// </summary>
public sealed record IdentitiesResponse
{
    /// <summary>
    /// Whether the user is anonymous (no linked identities).
    /// </summary>
    public required bool IsAnonymous { get; init; }

    /// <summary>
    /// When the user upgraded from anonymous (linked their first identity).
    /// </summary>
    public DateTimeOffset? LinkedAt { get; init; }

    /// <summary>
    /// Whether the user has a password linked.
    /// </summary>
    public required bool HasPassword { get; init; }

    /// <summary>
    /// The user's email address (if linked).
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    /// Whether the email is confirmed.
    /// </summary>
    public bool EmailConfirmed { get; init; }

    /// <summary>
    /// The linked OAuth providers.
    /// </summary>
    public required IReadOnlyCollection<LinkedProviderInfo> LinkedProviders { get; init; }

    /// <summary>
    /// The user's linked passkeys.
    /// </summary>
    public required IReadOnlyCollection<LinkedPasskeyInfo> LinkedPasskeys { get; init; }
}

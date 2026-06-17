namespace Application.Server.Identity.Models;

/// <summary>
/// Information about a linked external login provider.
/// </summary>
public sealed record ExternalLoginInfo
{
    /// <summary>
    /// The provider name (e.g., "Google", "GitHub").
    /// </summary>
    public required string Provider { get; init; }

    /// <summary>
    /// The user's unique identifier at the provider (subject claim).
    /// </summary>
    public required string ProviderSubject { get; init; }

    /// <summary>
    /// The display name for this linked account.
    /// </summary>
    public string? DisplayName { get; init; }

    /// <summary>
    /// The email associated with this linked account.
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    /// When this external login was linked.
    /// </summary>
    public required DateTimeOffset LinkedAt { get; init; }
}

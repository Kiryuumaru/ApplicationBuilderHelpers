namespace Application.Identity.Models;

/// <summary>
/// Read-only representation of an external login provider link.
/// </summary>
public sealed record ExternalLoginDto
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
    /// Display name for this linked account.
    /// </summary>
    public string? DisplayName { get; init; }

    /// <summary>
    /// Email associated with this linked account.
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    /// When this external login was linked.
    /// </summary>
    public DateTimeOffset LinkedAt { get; init; }
}

namespace Application.Identity.Models;

/// <summary>
/// Information about a registered passkey for display.
/// </summary>
public sealed record PasskeyInfo(
    Guid Id,
    string Name,
    DateTimeOffset RegisteredAt,
    DateTimeOffset? LastUsedAt
);

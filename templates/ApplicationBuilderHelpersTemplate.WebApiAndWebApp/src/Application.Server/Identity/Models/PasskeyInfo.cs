namespace Application.Server.Identity.Models;

/// <summary>
/// Information about a registered passkey for display.
/// </summary>
public record PasskeyInfo(
    Guid Id,
    string Name,
    DateTimeOffset RegisteredAt,
    DateTimeOffset? LastUsedAt
);

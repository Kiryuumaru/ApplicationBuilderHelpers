namespace Application.Server.Identity.Models;

/// <summary>
/// Result containing the challenge ID and JSON options for passkey authentication (WebAuthn assertion).
/// </summary>
public record PasskeyRequestOptions(
    Guid ChallengeId,
    string OptionsJson
);

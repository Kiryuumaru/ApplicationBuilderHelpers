namespace Application.Server.Identity.Models;

/// <summary>
/// Result containing the challenge ID and JSON options for passkey registration (WebAuthn attestation).
/// </summary>
public record PasskeyCreationOptions(
    Guid ChallengeId,
    string OptionsJson
);

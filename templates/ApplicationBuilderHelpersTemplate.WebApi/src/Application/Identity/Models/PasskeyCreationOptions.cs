namespace Application.Identity.Models;

/// <summary>
/// Result containing the challenge ID and JSON options for passkey registration (WebAuthn attestation).
/// </summary>
public sealed record PasskeyCreationOptions(
    Guid ChallengeId,
    string OptionsJson
);

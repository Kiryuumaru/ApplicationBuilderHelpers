namespace Application.Identity.Models;

/// <summary>
/// Result of passkey registration (attestation verification).
/// </summary>
public sealed record PasskeyRegistrationResult(
    Guid CredentialId,
    string CredentialName
);

namespace Application.Server.Identity.Models;

/// <summary>
/// Result of passkey registration (attestation verification).
/// </summary>
public record PasskeyRegistrationResult(
    Guid CredentialId,
    string CredentialName
);

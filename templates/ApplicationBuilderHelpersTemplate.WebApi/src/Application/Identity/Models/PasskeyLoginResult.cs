namespace Application.Identity.Models;

/// <summary>
/// Result of passkey authentication (assertion verification).
/// </summary>
public sealed record PasskeyLoginResult(
    UserSessionDto Session,
    Guid CredentialId
);

using Application.Server.Identity.Models;

namespace Application.Server.Identity.Interfaces.Inbound;

/// <summary>
/// Service for managing WebAuthn/FIDO2 passkey operations.
/// </summary>
public interface IPasskeyService
{
    /// <summary>
    /// Generates options for passkey registration (attestation).
    /// </summary>
    /// <param name="userId">The user ID to register a passkey for.</param>
    /// <param name="credentialName">User-friendly name for the passkey.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The challenge ID and WebAuthn options JSON.</returns>
    Task<PasskeyCreationOptions> GetRegistrationOptionsAsync(
        Guid userId,
        string credentialName,
        CancellationToken cancellationToken);

    /// <summary>
    /// Verifies and completes passkey registration.
    /// </summary>
    /// <param name="challengeId">The challenge ID from registration options.</param>
    /// <param name="attestationResponseJson">The attestation response from the authenticator.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The registration result with credential info.</returns>
    Task<PasskeyRegistrationResult> VerifyRegistrationAsync(
        Guid challengeId,
        string attestationResponseJson,
        CancellationToken cancellationToken);

    /// <summary>
    /// Generates options for passkey authentication (assertion).
    /// </summary>
    /// <param name="username">Optional username for non-discoverable credentials.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The challenge ID and WebAuthn options JSON.</returns>
    Task<PasskeyRequestOptions> GetLoginOptionsAsync(
        string? username,
        CancellationToken cancellationToken);

    /// <summary>
    /// Verifies and completes passkey authentication.
    /// </summary>
    /// <param name="challengeId">The challenge ID from login options.</param>
    /// <param name="assertionResponseJson">The assertion response from the authenticator.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The login result with user session and credential info.</returns>
    Task<PasskeyLoginResult> VerifyLoginAsync(
        Guid challengeId,
        string assertionResponseJson,
        CancellationToken cancellationToken);

    /// <summary>
    /// Lists all passkeys for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of passkey information.</returns>
    Task<IReadOnlyCollection<PasskeyInfo>> ListPasskeysAsync(
        Guid userId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Renames a passkey.
    /// </summary>
    /// <param name="userId">The user ID (for authorization).</param>
    /// <param name="credentialId">The passkey credential ID.</param>
    /// <param name="newName">The new name for the passkey.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RenamePasskeyAsync(
        Guid userId,
        Guid credentialId,
        string newName,
        CancellationToken cancellationToken);

    /// <summary>
    /// Revokes (deletes) a passkey.
    /// </summary>
    /// <param name="userId">The user ID (for authorization).</param>
    /// <param name="credentialId">The passkey credential ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RevokePasskeyAsync(
        Guid userId,
        Guid credentialId,
        CancellationToken cancellationToken);
}

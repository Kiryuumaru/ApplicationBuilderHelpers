using Application.Client.Identity.Models;

namespace Application.Client.Identity.Interfaces.Inbound;

/// <summary>
/// Interface for passkey (WebAuthn) operations.
/// </summary>
public interface IPasskeysClient
{
    /// <summary>
    /// Lists all passkeys for the specified user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of registered passkeys, or an error message.</returns>
    Task<(List<PasskeyInfo>? Passkeys, string? ErrorMessage)> ListPasskeysAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets options for creating a new passkey.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="credentialName">The name for the new passkey.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Registration options, or an error message.</returns>
    Task<(PasskeyRegistrationOptions? Options, string? ErrorMessage)> GetRegistrationOptionsAsync(Guid userId, string credentialName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a new passkey after browser attestation.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="challengeId">The challenge ID from GetRegistrationOptionsAsync.</param>
    /// <param name="attestationResponseJson">The JSON response from navigator.credentials.create().</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Registration result, or an error message.</returns>
    Task<(PasskeyRegistrationResult? Result, string? ErrorMessage)> RegisterPasskeyAsync(Guid userId, Guid challengeId, string attestationResponseJson, CancellationToken cancellationToken = default);

    /// <summary>
    /// Renames a passkey.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="credentialId">The passkey credential ID.</param>
    /// <param name="newName">The new name for the passkey.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if successful, or an error message.</returns>
    Task<(bool Success, string? ErrorMessage)> RenamePasskeyAsync(Guid userId, Guid credentialId, string newName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes/revokes a passkey.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="credentialId">The passkey credential ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if successful, or an error message.</returns>
    Task<(bool Success, string? ErrorMessage)> DeletePasskeyAsync(Guid userId, Guid credentialId, CancellationToken cancellationToken = default);
}

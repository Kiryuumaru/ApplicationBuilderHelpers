using Domain.Identity.Entities;
using Domain.Identity.Models;

namespace Application.Server.Identity.Interfaces.Outbound;

/// <summary>
/// Internal repository for passkey credential and challenge persistence operations.
/// Merges IPasskeyCredentialStore and IPasskeyChallengeStore into a single cohesive interface.
/// </summary>
public interface IPasskeyRepository
{
    // Credential operations (from IPasskeyCredentialStore)

    /// <summary>
    /// Saves a new passkey credential.
    /// </summary>
    Task SaveCredentialAsync(PasskeyCredential credential, CancellationToken cancellationToken);

    /// <summary>
    /// Gets all passkey credentials for a user.
    /// </summary>
    Task<IReadOnlyCollection<PasskeyCredential>> GetCredentialsByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a credential by its ID.
    /// </summary>
    Task<PasskeyCredential?> GetCredentialByIdAsync(Guid credentialId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a credential by its credential ID (the authenticator-assigned ID).
    /// </summary>
    Task<PasskeyCredential?> GetCredentialByCredentialIdAsync(byte[] credentialId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets all credentials matching any of the given credential IDs.
    /// </summary>
    Task<IReadOnlyCollection<PasskeyCredential>> GetCredentialsByCredentialIdsAsync(IEnumerable<byte[]> credentialIds, CancellationToken cancellationToken);

    /// <summary>
    /// Gets credentials by user handle (for discoverable credentials).
    /// </summary>
    Task<IReadOnlyCollection<PasskeyCredential>> GetCredentialsByUserHandleAsync(byte[] userHandle, CancellationToken cancellationToken);

    /// <summary>
    /// Updates an existing credential (e.g., sign count, last used).
    /// </summary>
    Task UpdateCredentialAsync(PasskeyCredential credential, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a passkey credential.
    /// </summary>
    Task DeleteCredentialAsync(Guid credentialId, CancellationToken cancellationToken);

    // Challenge operations (from IPasskeyChallengeStore)

    /// <summary>
    /// Saves a challenge for later verification.
    /// </summary>
    Task SaveChallengeAsync(PasskeyChallenge challenge, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a challenge by its ID.
    /// </summary>
    Task<PasskeyChallenge?> GetChallengeByIdAsync(Guid challengeId, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a challenge (typically after successful verification or expiration).
    /// </summary>
    Task DeleteChallengeAsync(Guid challengeId, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes all expired challenges (for cleanup).
    /// </summary>
    Task DeleteExpiredChallengesAsync(CancellationToken cancellationToken);
}

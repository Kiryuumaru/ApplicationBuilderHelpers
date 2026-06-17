using Domain.Identity.Entities;

namespace Domain.Identity.Interfaces;

/// <summary>
/// Repository for passkey credential and challenge persistence operations.
/// Changes are tracked but not persisted until IIdentityUnitOfWork.CommitAsync() is called.
/// </summary>
public interface IPasskeyRepository
{
    // Credential query methods
    Task<IReadOnlyCollection<PasskeyCredential>> GetCredentialsByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    Task<PasskeyCredential?> GetCredentialByIdAsync(Guid credentialId, CancellationToken cancellationToken);

    Task<PasskeyCredential?> GetCredentialByCredentialIdAsync(byte[] credentialId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<PasskeyCredential>> GetCredentialsByCredentialIdsAsync(IEnumerable<byte[]> credentialIds, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<PasskeyCredential>> GetCredentialsByUserHandleAsync(byte[] userHandle, CancellationToken cancellationToken);

    // Credential change tracking - changes are persisted on UnitOfWork.CommitAsync()
    void AddCredential(PasskeyCredential credential);

    void UpdateCredential(PasskeyCredential credential);

    void RemoveCredential(PasskeyCredential credential);

    // Challenge query methods
    Task<PasskeyChallenge?> GetChallengeByIdAsync(Guid challengeId, CancellationToken cancellationToken);

    // Challenge change tracking - changes are persisted on UnitOfWork.CommitAsync()
    void AddChallenge(PasskeyChallenge challenge);

    void RemoveChallenge(PasskeyChallenge challenge);

    // Bulk operation - executes immediately for efficiency (background cleanup)
    Task DeleteExpiredChallengesAsync(CancellationToken cancellationToken);
}

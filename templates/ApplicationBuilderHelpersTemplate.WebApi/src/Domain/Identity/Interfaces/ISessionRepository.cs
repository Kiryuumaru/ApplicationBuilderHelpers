using Domain.Identity.Entities;

namespace Domain.Identity.Interfaces;

/// <summary>
/// Repository for login session persistence operations.
/// Changes are tracked but not persisted until IIdentityUnitOfWork.CommitAsync() is called.
/// </summary>
public interface ISessionRepository
{
    // Query methods
    Task<LoginSession?> GetByIdAsync(Guid sessionId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<LoginSession>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    // Change tracking methods - changes are persisted on UnitOfWork.CommitAsync()
    void Add(LoginSession session);

    void Update(LoginSession session);

    void Remove(LoginSession session);

    // Bulk operations - execute immediately for efficiency
    Task<int> RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken);

    Task<int> RevokeAllExceptAsync(Guid userId, Guid exceptSessionId, CancellationToken cancellationToken);

    Task<int> DeleteExpiredAsync(DateTimeOffset olderThan, CancellationToken cancellationToken);
}

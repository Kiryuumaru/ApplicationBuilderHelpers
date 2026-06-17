using Domain.Identity.Entities;

namespace Domain.Identity.Interfaces;

/// <summary>
/// Repository for API key persistence operations.
/// Changes are tracked but not persisted until IIdentityUnitOfWork.CommitAsync() is called.
/// </summary>
public interface IApiKeyRepository
{
    // Query methods
    Task<ApiKey?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<ApiKey>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    Task<int> GetActiveCountByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    // Change tracking methods - changes are persisted on UnitOfWork.CommitAsync()
    void Add(ApiKey apiKey);

    void Update(ApiKey apiKey);

    void Remove(ApiKey apiKey);

    // Bulk operation - executes immediately for efficiency (background cleanup)
    Task<int> DeleteExpiredOrRevokedAsync(DateTimeOffset expiredBefore, DateTimeOffset revokedBefore, CancellationToken cancellationToken);
}

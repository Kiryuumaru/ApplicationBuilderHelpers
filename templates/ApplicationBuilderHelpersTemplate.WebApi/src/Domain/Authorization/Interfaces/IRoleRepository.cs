using Domain.Authorization.Entities;

namespace Domain.Authorization.Interfaces;

/// <summary>
/// Repository for role persistence operations.
/// Changes are tracked but not persisted until IAuthorizationUnitOfWork.CommitAsync() is called.
/// </summary>
public interface IRoleRepository
{
    // Query methods
    Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Role?> GetByCodeAsync(string code, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Role>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Role>> GetByCodesAsync(IEnumerable<string> codes, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Role>> ListAsync(CancellationToken cancellationToken);

    // Change tracking methods - changes are persisted on UnitOfWork.CommitAsync()
    void Add(Role role);

    void Update(Role role);

    void Remove(Role role);
}

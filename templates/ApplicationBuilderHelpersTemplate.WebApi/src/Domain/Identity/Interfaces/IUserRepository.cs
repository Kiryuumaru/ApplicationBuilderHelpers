using Domain.Identity.Enums;
using Domain.Identity.Models;
using Domain.Identity.Entities;

namespace Domain.Identity.Interfaces;

/// <summary>
/// Repository for user persistence operations.
/// Changes are tracked but not persisted until IIdentityUnitOfWork.CommitAsync() is called.
/// </summary>
public interface IUserRepository
{
    // Query methods
    Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<User?> FindByUsernameAsync(string username, CancellationToken cancellationToken);

    Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken);

    Task<User?> FindByExternalIdentityAsync(string provider, string providerSubject, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<User>> ListAsync(CancellationToken cancellationToken);

    Task<Guid?> FindUserByLoginAsync(
        ExternalLoginProvider provider,
        string providerKey,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ExternalLoginInfo>> GetLoginsAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<bool> HasAnyLoginAsync(Guid userId, CancellationToken cancellationToken);

    // Change tracking methods - changes are persisted on UnitOfWork.CommitAsync()
    void Add(User user);

    void Update(User user);

    void Remove(User user);

    void AddLogin(Guid userId, ExternalLoginProvider provider, string providerKey, string? displayName, string? email);

    void RemoveLogin(Guid userId, ExternalLoginProvider provider);

    // Bulk operation - executes immediately for efficiency (background cleanup)
    Task<int> DeleteAbandonedAnonymousUsersAsync(DateTimeOffset cutoffDate, CancellationToken cancellationToken);
}

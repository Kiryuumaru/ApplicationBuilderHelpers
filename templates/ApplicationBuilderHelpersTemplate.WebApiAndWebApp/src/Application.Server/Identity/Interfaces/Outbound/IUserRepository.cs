using Application.Server.Identity.Models;
using Domain.Identity.Enums;
using Domain.Identity.Entities;
using Domain.Identity.Models;

namespace Application.Server.Identity.Interfaces.Outbound;

/// <summary>
/// Internal repository for user persistence operations.
/// Merges IUserStore and IExternalLoginStore into a single cohesive interface.
/// </summary>
public interface IUserRepository
{
    // User operations
    Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<User?> FindByUsernameAsync(string username, CancellationToken cancellationToken);

    Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken);

    Task<User?> FindByExternalIdentityAsync(string provider, string providerSubject, CancellationToken cancellationToken);

    Task SaveAsync(User user, CancellationToken cancellationToken);

    Task DeleteAsync(Guid userId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<User>> ListAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Deletes anonymous users who have not been active since the specified cutoff date.
    /// </summary>
    /// <param name="cutoffDate">Users inactive before this date will be deleted.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of users deleted.</returns>
    Task<int> DeleteAbandonedAnonymousUsersAsync(DateTimeOffset cutoffDate, CancellationToken cancellationToken);

    // External login operations
    /// <summary>
    /// Finds a user by their external login.
    /// </summary>
    Task<Guid?> FindUserByLoginAsync(
        ExternalLoginProvider provider,
        string providerKey,
        CancellationToken cancellationToken);

    /// <summary>
    /// Adds an external login link for a user.
    /// </summary>
    Task AddLoginAsync(
        Guid userId,
        ExternalLoginProvider provider,
        string providerKey,
        string? displayName,
        string? email,
        CancellationToken cancellationToken);

    /// <summary>
    /// Removes an external login link from a user.
    /// </summary>
    Task<bool> RemoveLoginAsync(
        Guid userId,
        ExternalLoginProvider provider,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets all external logins for a user.
    /// </summary>
    Task<IReadOnlyCollection<ExternalLoginInfo>> GetLoginsAsync(
        Guid userId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Checks if a user has any external login linked.
    /// </summary>
    Task<bool> HasAnyLoginAsync(Guid userId, CancellationToken cancellationToken);
}

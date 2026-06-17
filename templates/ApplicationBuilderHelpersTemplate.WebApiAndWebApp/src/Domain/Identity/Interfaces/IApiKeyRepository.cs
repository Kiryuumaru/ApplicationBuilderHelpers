using Domain.Identity.Entities;

namespace Domain.Identity.Interfaces;

/// <summary>
/// Repository for API key persistence operations.
/// </summary>
public interface IApiKeyRepository
{
    /// <summary>
    /// Gets an API key by its ID.
    /// </summary>
    /// <param name="id">The API key ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The API key, or null if not found.</returns>
    Task<ApiKey?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Gets all non-revoked API keys for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of active API keys.</returns>
    Task<IReadOnlyList<ApiKey>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the count of active (non-revoked) API keys for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The count of active API keys.</returns>
    Task<int> GetActiveCountByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new API key.
    /// </summary>
    /// <param name="apiKey">The API key to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CreateAsync(ApiKey apiKey, CancellationToken cancellationToken);

    /// <summary>
    /// Updates an existing API key (e.g., LastUsedAt, IsRevoked).
    /// </summary>
    /// <param name="apiKey">The API key to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateAsync(ApiKey apiKey, CancellationToken cancellationToken);

    /// <summary>
    /// Hard deletes expired or old revoked API keys.
    /// Used by the cleanup worker.
    /// </summary>
    /// <param name="expiredBefore">Delete keys that expired before this date.</param>
    /// <param name="revokedBefore">Delete keys that were revoked before this date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of keys deleted.</returns>
    Task<int> DeleteExpiredOrRevokedAsync(DateTimeOffset expiredBefore, DateTimeOffset revokedBefore, CancellationToken cancellationToken);
}

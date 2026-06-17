using Application.Client.Identity.Models;

namespace Application.Client.Identity.Interfaces.Inbound;

/// <summary>
/// Interface for API key management operations.
/// </summary>
public interface IApiKeysClient
{
    /// <summary>
    /// Gets all API keys for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of API keys.</returns>
    Task<List<ApiKeyInfo>> ListApiKeysAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new API key.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="name">The API key name.</param>
    /// <param name="expiresAt">Optional expiration date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created API key with its secret token (shown only once).</returns>
    Task<CreateApiKeyResult?> CreateApiKeyAsync(Guid userId, string name, DateTimeOffset? expiresAt = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes an API key.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="apiKeyId">The API key ID to revoke.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if successful.</returns>
    Task<bool> RevokeApiKeyAsync(Guid userId, Guid apiKeyId, CancellationToken cancellationToken = default);
}

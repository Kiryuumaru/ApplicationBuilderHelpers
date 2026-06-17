using Application.Identity.Models;
using Domain.Identity.Models;
using Domain.Identity.Entities;

namespace Application.Identity.Interfaces.Inbound;

/// <summary>
/// Service for managing user API keys.
/// </summary>
public interface IApiKeyService
{
    /// <summary>
    /// Creates a new API key for a user.
    /// Returns the token (shown once) and metadata.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="name">User-friendly name for the API key.</param>
    /// <param name="expiresAt">Optional expiration date. Null means never expires.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple containing the API key metadata and the token string.</returns>
    Task<(ApiKeyDto Metadata, string Token)> CreateAsync(
        Guid userId,
        string name,
        DateTimeOffset? expiresAt,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets all non-revoked API keys for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of API key DTOs.</returns>
    Task<IReadOnlyList<ApiKeyDto>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a specific API key by ID.
    /// </summary>
    /// <param name="userId">The user ID (for ownership verification).</param>
    /// <param name="id">The API key ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The API key DTO, or null if not found or not owned by user.</returns>
    Task<ApiKeyDto?> GetByIdAsync(Guid userId, Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Revokes an API key (soft delete).
    /// </summary>
    /// <param name="userId">The user ID (for ownership verification).</param>
    /// <param name="id">The API key ID to revoke.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the key was found, belonged to the user, and was revoked; false otherwise.</returns>
    Task<bool> RevokeAsync(Guid userId, Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Validates an API key by its ID (from the keyId claim).
    /// Returns the API key if valid, null if revoked/expired/not found.
    /// </summary>
    /// <param name="keyId">The API key ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The API key if valid, null otherwise.</returns>
    Task<ApiKey?> ValidateApiKeyAsync(Guid keyId, CancellationToken cancellationToken);

    /// <summary>
    /// Updates the LastUsedAt timestamp for an API key.
    /// </summary>
    /// <param name="keyId">The API key ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateLastUsedAsync(Guid keyId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the count of active (non-revoked) API keys for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The count of active API keys.</returns>
    Task<int> GetActiveCountAsync(Guid userId, CancellationToken cancellationToken);
}

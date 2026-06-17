using Domain.Identity.Entities;
using Domain.Identity.Interfaces;
using Infrastructure.EFCore.Extensions;
using Infrastructure.EFCore.Server.Identity.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.EFCore.Server.Identity.Repositories;

internal sealed class EFCoreApiKeyRepository(IDbContextFactory<EFCoreDbContext> contextFactory) : IApiKeyRepository
{
    public async Task<ApiKey?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await context.Set<ApiKeyEntity>().FindAsync([id], cancellationToken);
        return entity is null ? null : MapToDomain(entity);
    }

    public async Task<IReadOnlyList<ApiKey>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var entities = await context.Set<ApiKeyEntity>()
            .Where(k => k.UserId == userId && !k.IsRevoked)
            .ToListAsync(cancellationToken);

        // SQLite doesn't support DateTimeOffset in ORDER BY, so sort in memory
        return entities
            .OrderByDescending(k => k.CreatedAt)
            .Select(MapToDomain)
            .ToList();
    }

    public async Task<int> GetActiveCountByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Set<ApiKeyEntity>()
            .CountAsync(k => k.UserId == userId && !k.IsRevoked, cancellationToken);
    }

    public async Task CreateAsync(ApiKey apiKey, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var entity = MapToEntity(apiKey);
        context.Set<ApiKeyEntity>().Add(entity);
        await context.SaveChangesWithExceptionHandlingAsync(cancellationToken);
    }

    public async Task UpdateAsync(ApiKey apiKey, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var entity = await context.Set<ApiKeyEntity>().FindAsync([apiKey.Id], cancellationToken);
        if (entity is null)
        {
            return;
        }

        entity.LastUsedAt = apiKey.LastUsedAt;
        entity.IsRevoked = apiKey.IsRevoked;
        entity.RevokedAt = apiKey.RevokedAt;

        await context.SaveChangesWithExceptionHandlingAsync(cancellationToken);
    }

    public async Task<int> DeleteExpiredOrRevokedAsync(
        DateTimeOffset expiredBefore,
        DateTimeOffset revokedBefore,
        CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        // Query expired keys and revoked keys separately to avoid complex expression translation issues
        var expiredKeys = await context.Set<ApiKeyEntity>()
            .Where(k => k.ExpiresAt != null && k.ExpiresAt < expiredBefore)
            .ToListAsync(cancellationToken);
        
        var revokedKeys = await context.Set<ApiKeyEntity>()
            .Where(k => k.IsRevoked && k.RevokedAt != null && k.RevokedAt < revokedBefore)
            .ToListAsync(cancellationToken);
        
        var keysToDelete = expiredKeys.Union(revokedKeys).ToList();

        if (keysToDelete.Count == 0)
        {
            return 0;
        }

        context.Set<ApiKeyEntity>().RemoveRange(keysToDelete);
        await context.SaveChangesWithExceptionHandlingAsync(cancellationToken);
        return keysToDelete.Count;
    }

    private static ApiKeyEntity MapToEntity(ApiKey apiKey) => new()
    {
        Id = apiKey.Id,
        UserId = apiKey.UserId,
        Name = apiKey.Name,
        CreatedAt = apiKey.CreatedAt,
        ExpiresAt = apiKey.ExpiresAt,
        LastUsedAt = apiKey.LastUsedAt,
        IsRevoked = apiKey.IsRevoked,
        RevokedAt = apiKey.RevokedAt
    };

    private static ApiKey MapToDomain(ApiKeyEntity entity) => ApiKey.Reconstruct(
        entity.Id,
        entity.UserId,
        entity.Name,
        entity.CreatedAt,
        entity.ExpiresAt,
        entity.LastUsedAt,
        entity.IsRevoked,
        entity.RevokedAt);
}

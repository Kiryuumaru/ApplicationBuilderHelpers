using Domain.Identity.Entities;
using Domain.Identity.Interfaces;
using Infrastructure.EFCore.Extensions;
using Infrastructure.EFCore.Identity.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.EFCore.Identity.Repositories;

internal sealed class EFCoreApiKeyRepository(EFCoreDbContext context) : IApiKeyRepository
{
    // Query methods

    public async Task<ApiKey?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await context.Set<ApiKeyEntity>().FindAsync([id], cancellationToken);
        return entity is null ? null : MapToDomain(entity);
    }

    public async Task<IReadOnlyList<ApiKey>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var entities = await context.Set<ApiKeyEntity>()
            .Where(k => k.UserId == userId && !k.IsRevoked)
            .ToListAsync(cancellationToken);

        return entities
            .OrderByDescending(k => k.CreatedAt)
            .Select(MapToDomain)
            .ToList();
    }

    public async Task<int> GetActiveCountByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await context.Set<ApiKeyEntity>()
            .CountAsync(k => k.UserId == userId && !k.IsRevoked, cancellationToken);
    }

    // Change tracking methods - changes are persisted on UnitOfWork.CommitAsync()

    public void Add(ApiKey apiKey)
    {
        var entity = MapToEntity(apiKey);
        context.Set<ApiKeyEntity>().Add(entity);
    }

    public void Update(ApiKey apiKey)
    {
        var entity = context.Set<ApiKeyEntity>().Local.FirstOrDefault(e => e.Id == apiKey.Id);
        if (entity is not null)
        {
            entity.LastUsedAt = apiKey.LastUsedAt;
            entity.IsRevoked = apiKey.IsRevoked;
            entity.RevokedAt = apiKey.RevokedAt;
        }
        else
        {
            entity = MapToEntity(apiKey);
            context.Set<ApiKeyEntity>().Attach(entity);
            context.Entry(entity).State = EntityState.Modified;
        }
    }

    public void Remove(ApiKey apiKey)
    {
        var entity = context.Set<ApiKeyEntity>().Local.FirstOrDefault(e => e.Id == apiKey.Id)
            ?? MapToEntity(apiKey);
        context.Set<ApiKeyEntity>().Remove(entity);
    }

    // Bulk operation - executes immediately for efficiency (background cleanup)

    public async Task<int> DeleteExpiredOrRevokedAsync(
        DateTimeOffset expiredBefore,
        DateTimeOffset revokedBefore,
        CancellationToken cancellationToken)
    {
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

    // Helper methods

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

    private static ApiKey MapToDomain(ApiKeyEntity entity) => ApiKey.Hydrate(
        entity.Id,
        entity.UserId,
        entity.Name,
        entity.CreatedAt,
        entity.ExpiresAt,
        entity.LastUsedAt,
        entity.IsRevoked,
        entity.RevokedAt);
}

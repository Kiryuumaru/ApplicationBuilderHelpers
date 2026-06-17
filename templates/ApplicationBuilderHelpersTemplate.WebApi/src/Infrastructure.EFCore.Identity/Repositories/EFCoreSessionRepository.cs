using Domain.Identity.Entities;
using Domain.Identity.Interfaces;
using Infrastructure.EFCore.Extensions;
using Infrastructure.EFCore.Identity.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.EFCore.Identity.Repositories;

internal sealed class EFCoreSessionRepository(EFCoreDbContext context) : ISessionRepository
{
    // Query methods

    public async Task<LoginSession?> GetByIdAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var entity = await context.Set<LoginSessionEntity>().FindAsync([sessionId], cancellationToken);
        return entity is null ? null : MapToDomain(entity);
    }

    public async Task<IReadOnlyCollection<LoginSession>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var entities = await context.Set<LoginSessionEntity>()
            .Where(s => s.UserId == userId && !s.IsRevoked)
            .ToListAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        return entities
            .Where(s => s.ExpiresAt > now)
            .OrderByDescending(s => s.LastUsedAt)
            .Select(MapToDomain)
            .ToList();
    }

    // Change tracking methods - changes are persisted on UnitOfWork.CommitAsync()

    public void Add(LoginSession session)
    {
        var entity = MapToEntity(session);
        context.Set<LoginSessionEntity>().Add(entity);
    }

    public void Update(LoginSession session)
    {
        var entity = context.Set<LoginSessionEntity>().Local.FirstOrDefault(e => e.Id == session.Id);
        if (entity is not null)
        {
            entity.RefreshTokenHash = session.RefreshTokenHash;
            entity.LastUsedAt = session.LastUsedAt;
            entity.ExpiresAt = session.ExpiresAt;
            entity.IsRevoked = session.IsRevoked;
            entity.RevokedAt = session.RevokedAt;
        }
        else
        {
            entity = MapToEntity(session);
            context.Set<LoginSessionEntity>().Attach(entity);
            context.Entry(entity).State = EntityState.Modified;
        }
    }

    public void Remove(LoginSession session)
    {
        var entity = context.Set<LoginSessionEntity>().Local.FirstOrDefault(e => e.Id == session.Id)
            ?? MapToEntity(session);
        context.Set<LoginSessionEntity>().Remove(entity);
    }

    // Bulk operations - execute immediately for efficiency

    public async Task<int> RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var sessions = await context.Set<LoginSessionEntity>()
            .Where(s => s.UserId == userId && !s.IsRevoked)
            .ToListAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        foreach (var session in sessions)
        {
            session.IsRevoked = true;
            session.RevokedAt = now;
        }

        await context.SaveChangesWithExceptionHandlingAsync(cancellationToken);
        return sessions.Count;
    }

    public async Task<int> RevokeAllExceptAsync(Guid userId, Guid exceptSessionId, CancellationToken cancellationToken)
    {
        var sessions = await context.Set<LoginSessionEntity>()
            .Where(s => s.UserId == userId && !s.IsRevoked && s.Id != exceptSessionId)
            .ToListAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        foreach (var session in sessions)
        {
            session.IsRevoked = true;
            session.RevokedAt = now;
        }

        await context.SaveChangesWithExceptionHandlingAsync(cancellationToken);
        return sessions.Count;
    }

    public async Task<int> DeleteExpiredAsync(DateTimeOffset olderThan, CancellationToken cancellationToken)
    {
        var expiredSessions = await context.Set<LoginSessionEntity>()
            .ToListAsync(cancellationToken);

        var toDelete = expiredSessions
            .Where(s => (s.IsRevoked && s.RevokedAt < olderThan) || s.ExpiresAt < olderThan)
            .ToList();

        context.Set<LoginSessionEntity>().RemoveRange(toDelete);
        await context.SaveChangesWithExceptionHandlingAsync(cancellationToken);
        return toDelete.Count;
    }

    // Helper methods

    private static LoginSessionEntity MapToEntity(LoginSession session)
    {
        return new LoginSessionEntity
        {
            Id = session.Id,
            UserId = session.UserId,
            RefreshTokenHash = session.RefreshTokenHash,
            DeviceName = session.DeviceName,
            UserAgent = session.UserAgent,
            IpAddress = session.IpAddress,
            CreatedAt = session.CreatedAt,
            ExpiresAt = session.ExpiresAt,
            LastUsedAt = session.LastUsedAt,
            IsRevoked = session.IsRevoked,
            RevokedAt = session.RevokedAt
        };
    }

    private static LoginSession MapToDomain(LoginSessionEntity entity)
    {
        return LoginSession.Reconstruct(
            entity.Id,
            entity.UserId,
            entity.RefreshTokenHash,
            entity.DeviceName,
            entity.UserAgent,
            entity.IpAddress,
            entity.CreatedAt,
            entity.LastUsedAt,
            entity.ExpiresAt,
            entity.IsRevoked,
            entity.RevokedAt);
    }
}

using Domain.Identity.Entities;
using Domain.Identity.Interfaces;
using Domain.Shared.Exceptions;
using Infrastructure.EFCore.Extensions;
using Infrastructure.EFCore.Server.Identity.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.EFCore.Server.Identity.Repositories;

internal sealed class EFCoreSessionRepository(IDbContextFactory<EFCoreDbContext> contextFactory) : ISessionRepository
{
    public async Task CreateAsync(LoginSession session, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var entity = MapToEntity(session);
        context.Set<LoginSessionEntity>().Add(entity);
        await context.SaveChangesWithExceptionHandlingAsync(cancellationToken);
    }

    public async Task<LoginSession?> GetByIdAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await context.Set<LoginSessionEntity>().FindAsync([sessionId], cancellationToken);
        return entity is null ? null : MapToDomain(entity);
    }

    public async Task<IReadOnlyCollection<LoginSession>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        
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

    public async Task UpdateAsync(LoginSession session, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        
        var entity = await context.Set<LoginSessionEntity>().FindAsync([session.Id], cancellationToken)
            ?? throw new EntityNotFoundException(nameof(LoginSession), session.Id.ToString());

        entity.RefreshTokenHash = session.RefreshTokenHash;
        entity.LastUsedAt = session.LastUsedAt;
        entity.ExpiresAt = session.ExpiresAt;
        entity.IsRevoked = session.IsRevoked;
        entity.RevokedAt = session.RevokedAt;

        await context.SaveChangesWithExceptionHandlingAsync(cancellationToken);
    }

    public async Task<bool> RevokeAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        
        var entity = await context.Set<LoginSessionEntity>().FindAsync([sessionId], cancellationToken);
        if (entity is null || entity.IsRevoked)
        {
            return false;
        }

        entity.IsRevoked = true;
        entity.RevokedAt = DateTimeOffset.UtcNow;
        await context.SaveChangesWithExceptionHandlingAsync(cancellationToken);
        return true;
    }

    public async Task<int> RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        
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
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        
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
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        
        var expiredSessions = await context.Set<LoginSessionEntity>()
            .ToListAsync(cancellationToken);

        var toDelete = expiredSessions
            .Where(s => (s.IsRevoked && s.RevokedAt < olderThan) || s.ExpiresAt < olderThan)
            .ToList();

        context.Set<LoginSessionEntity>().RemoveRange(toDelete);
        await context.SaveChangesWithExceptionHandlingAsync(cancellationToken);
        return toDelete.Count;
    }

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

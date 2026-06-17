using Application.Server.Identity.Interfaces.Outbound;
using Domain.Identity.Enums;
using Domain.Identity.Entities;
using Domain.Identity.Models;
using Domain.Shared.Exceptions;
using Infrastructure.EFCore.Extensions;
using Infrastructure.EFCore.Server.Identity.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.EFCore.Server.Identity.Repositories;

internal sealed class EFCorePasskeyRepository(IDbContextFactory<EFCoreDbContext> contextFactory) : IPasskeyRepository
{
    // Credential operations (from IPasskeyCredentialStore)

    public async Task SaveCredentialAsync(PasskeyCredential credential, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var entity = MapCredentialToEntity(credential);
        context.Set<PasskeyCredentialEntity>().Add(entity);
        await context.SaveChangesWithExceptionHandlingAsync("PasskeyCredential", credential.Id.ToString(), cancellationToken);
    }

    public async Task<IReadOnlyCollection<PasskeyCredential>> GetCredentialsByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var entities = await context.Set<PasskeyCredentialEntity>()
            .Where(c => c.UserId == userId)
            .ToListAsync(cancellationToken);

        return entities.Select(MapCredentialToDomain).ToList();
    }

    public async Task<PasskeyCredential?> GetCredentialByIdAsync(Guid credentialId, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await context.Set<PasskeyCredentialEntity>().FindAsync([credentialId], cancellationToken);
        return entity is null ? null : MapCredentialToDomain(entity);
    }

    public async Task<PasskeyCredential?> GetCredentialByCredentialIdAsync(byte[] credentialId, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await context.Set<PasskeyCredentialEntity>()
            .FirstOrDefaultAsync(c => c.CredentialId == credentialId, cancellationToken);

        return entity is null ? null : MapCredentialToDomain(entity);
    }

    public async Task<IReadOnlyCollection<PasskeyCredential>> GetCredentialsByCredentialIdsAsync(IEnumerable<byte[]> credentialIds, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var idList = credentialIds.ToList();
        if (idList.Count == 0)
        {
            return [];
        }

        // EF Core may not support Contains for byte arrays directly
        // We'll fetch all and filter in memory for now
        var entities = await context.Set<PasskeyCredentialEntity>().ToListAsync(cancellationToken);
        var matching = entities
            .Where(e => idList.Any(id => id.SequenceEqual(e.CredentialId)))
            .Select(MapCredentialToDomain)
            .ToList();

        return matching;
    }

    public async Task<IReadOnlyCollection<PasskeyCredential>> GetCredentialsByUserHandleAsync(byte[] userHandle, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var entities = await context.Set<PasskeyCredentialEntity>()
            .Where(c => c.UserHandle == userHandle)
            .ToListAsync(cancellationToken);

        return entities.Select(MapCredentialToDomain).ToList();
    }

    public async Task UpdateCredentialAsync(PasskeyCredential credential, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await context.Set<PasskeyCredentialEntity>().FindAsync([credential.Id], cancellationToken)
            ?? throw new EntityNotFoundException(nameof(PasskeyCredential), credential.Id.ToString());

        entity.Name = credential.Name;
        entity.SignCount = credential.SignCount;
        entity.LastUsedAt = credential.LastUsedAt;

        await context.SaveChangesWithExceptionHandlingAsync(cancellationToken);
    }

    public async Task DeleteCredentialAsync(Guid credentialId, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await context.Set<PasskeyCredentialEntity>().FindAsync([credentialId], cancellationToken);
        if (entity is not null)
        {
            context.Set<PasskeyCredentialEntity>().Remove(entity);
            await context.SaveChangesWithExceptionHandlingAsync(cancellationToken);
        }
    }

    // Challenge operations (from IPasskeyChallengeStore)

    public async Task SaveChallengeAsync(PasskeyChallenge challenge, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var entity = MapChallengeToEntity(challenge);
        context.Set<PasskeyChallengeEntity>().Add(entity);
        await context.SaveChangesWithExceptionHandlingAsync(cancellationToken);
    }

    public async Task<PasskeyChallenge?> GetChallengeByIdAsync(Guid challengeId, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await context.Set<PasskeyChallengeEntity>().FindAsync([challengeId], cancellationToken);
        return entity is null ? null : MapChallengeToDomain(entity);
    }

    public async Task DeleteChallengeAsync(Guid challengeId, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await context.Set<PasskeyChallengeEntity>().FindAsync([challengeId], cancellationToken);
        if (entity is not null)
        {
            context.Set<PasskeyChallengeEntity>().Remove(entity);
            await context.SaveChangesWithExceptionHandlingAsync(cancellationToken);
        }
    }

    public async Task DeleteExpiredChallengesAsync(CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var expired = await context.Set<PasskeyChallengeEntity>()
            .Where(c => c.ExpiresAt <= now)
            .ToListAsync(cancellationToken);

        if (expired.Count > 0)
        {
            context.Set<PasskeyChallengeEntity>().RemoveRange(expired);
            await context.SaveChangesWithExceptionHandlingAsync(cancellationToken);
        }
    }

    // Mapping methods

    private static PasskeyCredentialEntity MapCredentialToEntity(PasskeyCredential credential)
    {
        return new PasskeyCredentialEntity
        {
            Id = credential.Id,
            UserId = credential.UserId,
            Name = credential.Name,
            CredentialId = credential.CredentialId,
            PublicKey = credential.PublicKey,
            UserHandle = credential.UserHandle,
            SignCount = credential.SignCount,
            AaGuid = credential.AaGuid,
            CredentialType = credential.CredentialType,
            AttestationFormat = credential.AttestationFormat,
            RegisteredAt = credential.RegisteredAt,
            LastUsedAt = credential.LastUsedAt
        };
    }

    private static PasskeyCredential MapCredentialToDomain(PasskeyCredentialEntity entity)
    {
        return PasskeyCredential.Reconstruct(
            entity.Id,
            entity.UserId,
            entity.Name,
            entity.CredentialId,
            entity.PublicKey,
            entity.SignCount,
            entity.AaGuid,
            entity.CredentialType,
            entity.UserHandle,
            entity.AttestationFormat,
            entity.RegisteredAt,
            entity.LastUsedAt);
    }

    private static PasskeyChallengeEntity MapChallengeToEntity(PasskeyChallenge challenge)
    {
        return new PasskeyChallengeEntity
        {
            Id = challenge.Id,
            Challenge = challenge.Challenge,
            UserId = challenge.UserId,
            Type = (int)challenge.Type,
            OptionsJson = challenge.OptionsJson,
            CredentialName = challenge.CredentialName,
            CreatedAt = challenge.CreatedAt,
            ExpiresAt = challenge.ExpiresAt
        };
    }

    private static PasskeyChallenge MapChallengeToDomain(PasskeyChallengeEntity entity)
    {
        return PasskeyChallenge.Reconstruct(
            entity.Id,
            entity.Challenge,
            entity.UserId,
            (PasskeyChallengeType)entity.Type,
            entity.OptionsJson,
            entity.CredentialName,
            entity.CreatedAt,
            entity.ExpiresAt);
    }
}

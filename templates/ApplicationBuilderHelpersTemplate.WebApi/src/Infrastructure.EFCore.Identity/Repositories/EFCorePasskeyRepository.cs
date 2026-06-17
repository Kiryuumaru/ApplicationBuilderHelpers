using Domain.Identity.Enums;
using Domain.Identity.Interfaces;
using Domain.Identity.Models;
using Domain.Identity.Entities;
using Infrastructure.EFCore.Extensions;
using Infrastructure.EFCore.Identity.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.EFCore.Identity.Repositories;

internal sealed class EFCorePasskeyRepository(EFCoreDbContext context) : IPasskeyRepository
{
    // Credential query methods

    public async Task<IReadOnlyCollection<PasskeyCredential>> GetCredentialsByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var entities = await context.Set<PasskeyCredentialEntity>()
            .Where(c => c.UserId == userId)
            .ToListAsync(cancellationToken);

        return entities.Select(MapCredentialToDomain).ToList();
    }

    public async Task<PasskeyCredential?> GetCredentialByIdAsync(Guid credentialId, CancellationToken cancellationToken)
    {
        var entity = await context.Set<PasskeyCredentialEntity>().FindAsync([credentialId], cancellationToken);
        return entity is null ? null : MapCredentialToDomain(entity);
    }

    public async Task<PasskeyCredential?> GetCredentialByCredentialIdAsync(byte[] credentialId, CancellationToken cancellationToken)
    {
        var entity = await context.Set<PasskeyCredentialEntity>()
            .FirstOrDefaultAsync(c => c.CredentialId == credentialId, cancellationToken);

        return entity is null ? null : MapCredentialToDomain(entity);
    }

    public async Task<IReadOnlyCollection<PasskeyCredential>> GetCredentialsByCredentialIdsAsync(IEnumerable<byte[]> credentialIds, CancellationToken cancellationToken)
    {
        var idList = credentialIds.ToList();
        if (idList.Count == 0)
        {
            return [];
        }

        var entities = await context.Set<PasskeyCredentialEntity>().ToListAsync(cancellationToken);
        var matching = entities
            .Where(e => idList.Any(id => id.SequenceEqual(e.CredentialId)))
            .Select(MapCredentialToDomain)
            .ToList();

        return matching;
    }

    public async Task<IReadOnlyCollection<PasskeyCredential>> GetCredentialsByUserHandleAsync(byte[] userHandle, CancellationToken cancellationToken)
    {
        var entities = await context.Set<PasskeyCredentialEntity>()
            .Where(c => c.UserHandle == userHandle)
            .ToListAsync(cancellationToken);

        return entities.Select(MapCredentialToDomain).ToList();
    }

    // Credential change tracking - changes are persisted on UnitOfWork.CommitAsync()

    public void AddCredential(PasskeyCredential credential)
    {
        var entity = MapCredentialToEntity(credential);
        context.Set<PasskeyCredentialEntity>().Add(entity);
    }

    public void UpdateCredential(PasskeyCredential credential)
    {
        var entity = context.Set<PasskeyCredentialEntity>().Local.FirstOrDefault(e => e.Id == credential.Id);
        if (entity is not null)
        {
            entity.Name = credential.Name;
            entity.SignCount = credential.SignCount;
            entity.LastUsedAt = credential.LastUsedAt;
        }
        else
        {
            entity = MapCredentialToEntity(credential);
            context.Set<PasskeyCredentialEntity>().Attach(entity);
            context.Entry(entity).State = EntityState.Modified;
        }
    }

    public void RemoveCredential(PasskeyCredential credential)
    {
        var entity = context.Set<PasskeyCredentialEntity>().Local.FirstOrDefault(e => e.Id == credential.Id)
            ?? MapCredentialToEntity(credential);
        context.Set<PasskeyCredentialEntity>().Remove(entity);
    }

    // Challenge query methods

    public async Task<PasskeyChallenge?> GetChallengeByIdAsync(Guid challengeId, CancellationToken cancellationToken)
    {
        var entity = await context.Set<PasskeyChallengeEntity>().FindAsync([challengeId], cancellationToken);
        return entity is null ? null : MapChallengeToDomain(entity);
    }

    // Challenge change tracking - changes are persisted on UnitOfWork.CommitAsync()

    public void AddChallenge(PasskeyChallenge challenge)
    {
        var entity = MapChallengeToEntity(challenge);
        context.Set<PasskeyChallengeEntity>().Add(entity);
    }

    public void RemoveChallenge(PasskeyChallenge challenge)
    {
        var entity = context.Set<PasskeyChallengeEntity>().Local.FirstOrDefault(e => e.Id == challenge.Id)
            ?? MapChallengeToEntity(challenge);
        context.Set<PasskeyChallengeEntity>().Remove(entity);
    }

    // Bulk operation - executes immediately for efficiency (background cleanup)

    public async Task DeleteExpiredChallengesAsync(CancellationToken cancellationToken)
    {
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

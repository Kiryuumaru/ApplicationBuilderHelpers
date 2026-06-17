using System.Text.Json;
using Domain.Authorization.Entities;
using Domain.Authorization.Exceptions;
using Domain.Authorization.Interfaces;
using Infrastructure.EFCore.Identity.Models;
using Microsoft.EntityFrameworkCore;
using RolesConstants = Domain.Authorization.Constants.Roles;

namespace Infrastructure.EFCore.Identity.Repositories;

internal sealed class EFCoreRoleRepository(EFCoreDbContext context) : IRoleRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public async Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        // Check static roles first
        if (RolesConstants.TryGetById(id, out var staticRole))
        {
            return staticRole;
        }

        var entity = await context.Set<RoleEntity>().FindAsync([id], cancellationToken);
        return entity is null ? null : MapToDomain(entity);
    }

    public async Task<Role?> GetByCodeAsync(string code, CancellationToken cancellationToken)
    {
        // Check static roles first
        if (RolesConstants.TryGetByCode(code, out var staticRole))
        {
            return staticRole;
        }

        var normalizedCode = code.Trim().ToUpperInvariant();
        var entity = await context.Set<RoleEntity>()
            .FirstOrDefaultAsync(r => r.Code == normalizedCode, cancellationToken);
        return entity is null ? null : MapToDomain(entity);
    }

    public async Task<IReadOnlyCollection<Role>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken)
    {
        var idList = ids.Distinct().ToArray();
        if (idList.Length == 0)
        {
            return [];
        }

        var result = new Dictionary<Guid, Role>();
        var remainingIds = new List<Guid>();

        // Check static roles first
        foreach (var id in idList)
        {
            if (RolesConstants.TryGetById(id, out var staticRole))
            {
                result[id] = staticRole;
            }
            else
            {
                remainingIds.Add(id);
            }
        }

        // Query database for non-static roles
        if (remainingIds.Count > 0)
        {
            var dbEntities = await context.Set<RoleEntity>()
                .Where(r => remainingIds.Contains(r.Id))
                .ToListAsync(cancellationToken);

            foreach (var entity in dbEntities)
            {
                result[entity.Id] = MapToDomain(entity);
            }
        }

        return [.. result.Values];
    }

    public async Task<IReadOnlyCollection<Role>> GetByCodesAsync(IEnumerable<string> codes, CancellationToken cancellationToken)
    {
        var codeList = codes
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.Trim().ToUpperInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (codeList.Length == 0)
        {
            return [];
        }

        var result = new Dictionary<string, Role>(StringComparer.OrdinalIgnoreCase);
        var remainingCodes = new List<string>();

        // Check static roles first
        foreach (var code in codeList)
        {
            if (RolesConstants.TryGetByCode(code, out var staticRole))
            {
                result[code] = staticRole;
            }
            else
            {
                remainingCodes.Add(code);
            }
        }

        // Query database for non-static roles
        if (remainingCodes.Count > 0)
        {
            var dbEntities = await context.Set<RoleEntity>()
                .Where(r => remainingCodes.Contains(r.Code))
                .ToListAsync(cancellationToken);

            foreach (var entity in dbEntities)
            {
                result[entity.Code] = MapToDomain(entity);
            }
        }

        return [.. result.Values];
    }

    public async Task<IReadOnlyCollection<Role>> ListAsync(CancellationToken cancellationToken)
    {
        var result = new Dictionary<Guid, Role>();

        // Add all static roles first
        foreach (var role in RolesConstants.AllRoles)
        {
            result[role.Id] = role;
        }

        // Add database roles (excluding duplicates of static roles)
        var dbEntities = await context.Set<RoleEntity>().ToListAsync(cancellationToken);
        foreach (var entity in dbEntities)
        {
            if (!RolesConstants.IsStaticRole(entity.Id))
            {
                result[entity.Id] = MapToDomain(entity);
            }
        }

        return [.. result.Values];
    }

    // Change tracking - changes are persisted on UnitOfWork.CommitAsync()

    public void Add(Role role)
    {
        // Static roles cannot be saved - they are defined in code
        if (RolesConstants.IsStaticRole(role.Id))
        {
            throw new SystemRoleException($"Cannot save static role '{role.Code}'. Static roles are defined in code.", role.Code);
        }

        var entity = MapToEntity(role);
        context.Set<RoleEntity>().Add(entity);
    }

    public void Update(Role role)
    {
        // Static roles cannot be updated - they are defined in code
        if (RolesConstants.IsStaticRole(role.Id))
        {
            throw new SystemRoleException($"Cannot update static role '{role.Code}'. Static roles are defined in code.", role.Code);
        }

        var entity = context.Set<RoleEntity>().Local.FirstOrDefault(e => e.Id == role.Id);
        if (entity is not null)
        {
            entity.RevId = role.RevId;
            entity.Code = role.Code;
            entity.Name = role.Name;
            entity.NormalizedName = role.NormalizedName;
            entity.Description = role.Description;
            entity.ScopeTemplatesJson = SerializeScopeTemplates(role);
        }
        else
        {
            entity = MapToEntity(role);
            context.Set<RoleEntity>().Attach(entity);
            context.Entry(entity).State = EntityState.Modified;
        }
    }

    public void Remove(Role role)
    {
        // Static roles cannot be deleted - they are defined in code
        if (RolesConstants.IsStaticRole(role.Id))
        {
            throw new SystemRoleException($"Cannot delete static role '{role.Code}'. Static roles are defined in code.", role.Code);
        }

        var entity = context.Set<RoleEntity>().Local.FirstOrDefault(e => e.Id == role.Id)
            ?? MapToEntity(role);
        context.Set<RoleEntity>().Remove(entity);
    }

    // Mapping methods

    private static Role MapToDomain(RoleEntity entity)
    {
        IEnumerable<Domain.Authorization.ValueObjects.ScopeTemplate>? scopeTemplates = null;

        if (!string.IsNullOrWhiteSpace(entity.ScopeTemplatesJson))
        {
            var dtos = JsonSerializer.Deserialize<List<ScopeTemplateDto>>(entity.ScopeTemplatesJson, JsonOptions);
            if (dtos is not null)
            {
                scopeTemplates = dtos.Select(d => d.ToDomain());
            }
        }

        return Role.Hydrate(
            entity.Id,
            entity.RevId,
            entity.Code,
            entity.Name,
            entity.Description,
            isSystemRole: false, // Database roles are never system roles
            scopeTemplates);
    }

    private static RoleEntity MapToEntity(Role role)
    {
        return new RoleEntity
        {
            Id = role.Id,
            RevId = role.RevId,
            Code = role.Code,
            Name = role.Name,
            NormalizedName = role.NormalizedName,
            Description = role.Description,
            ScopeTemplatesJson = SerializeScopeTemplates(role)
        };
    }

    private static string? SerializeScopeTemplates(Role role)
    {
        if (role.ScopeTemplates.Count == 0)
        {
            return null;
        }

        var dtos = role.ScopeTemplates.Select(ScopeTemplateDto.FromDomain).ToList();
        return JsonSerializer.Serialize(dtos, JsonOptions);
    }
}

using System.Text.Json;
using Application.Server.Authorization.Interfaces.Outbound;
using Domain.Authorization.Entities;
using Domain.Authorization.Exceptions;
using Domain.Authorization.Models;
using Domain.Authorization.ValueObjects;
using Infrastructure.EFCore.Extensions;
using Infrastructure.EFCore.Server.Identity.Serialization;
using Infrastructure.EFCore.Server.Identity.Models;
using Microsoft.EntityFrameworkCore;
using RolesConstants = Domain.Authorization.Constants.Roles;

namespace Infrastructure.EFCore.Server.Identity.Repositories;

internal sealed class EFCoreRoleRepository(IDbContextFactory<EFCoreDbContext> contextFactory) : IRoleRepository
{

    public async Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        // Check static roles first
        if (RolesConstants.TryGetById(id, out var staticRole))
        {
            return staticRole;
        }

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await context.Set<RoleEntity>().FindAsync([id], cancellationToken);
        return entity is null ? null : ToDomain(entity);
    }

    public async Task<Role?> GetByCodeAsync(string code, CancellationToken cancellationToken)
    {
        // Check static roles first
        if (RolesConstants.TryGetByCode(code, out var staticRole))
        {
            return staticRole;
        }

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var normalizedCode = code.Trim().ToUpperInvariant();
        var entity = await context.Set<RoleEntity>()
            .FirstOrDefaultAsync(r => r.Code == normalizedCode, cancellationToken);
        return entity is null ? null : ToDomain(entity);
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
            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
            var dbEntities = await context.Set<RoleEntity>()
                .Where(r => remainingIds.Contains(r.Id))
                .ToListAsync(cancellationToken);
            
            foreach (var entity in dbEntities)
            {
                result[entity.Id] = ToDomain(entity);
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
            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
            var dbEntities = await context.Set<RoleEntity>()
                .Where(r => remainingCodes.Contains(r.Code))
                .ToListAsync(cancellationToken);
            
            foreach (var entity in dbEntities)
            {
                result[entity.Code] = ToDomain(entity);
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
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var dbEntities = await context.Set<RoleEntity>().ToListAsync(cancellationToken);
        foreach (var entity in dbEntities)
        {
            if (!RolesConstants.IsStaticRole(entity.Id))
            {
                result[entity.Id] = ToDomain(entity);
            }
        }

        return [.. result.Values];
    }

    public async Task SaveAsync(Role role, CancellationToken cancellationToken)
    {
        // Static roles cannot be saved - they are defined in code
        if (RolesConstants.IsStaticRole(role.Id))
        {
            throw new SystemRoleException($"Cannot save static role '{role.Code}'. Static roles are defined in code.", role.Code);
        }

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        
        var entity = ToEntity(role);
        var existing = await context.Set<RoleEntity>().FindAsync([role.Id], cancellationToken);
        if (existing is null)
        {
            context.Set<RoleEntity>().Add(entity);
        }
        else
        {
            context.Entry(existing).CurrentValues.SetValues(entity);
        }

        await context.SaveChangesWithExceptionHandlingAsync("Role", role.Code, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        // Static roles cannot be deleted
        if (RolesConstants.IsStaticRole(id))
        {
            throw new SystemRoleException($"Cannot delete static role. Static roles are defined in code.");
        }

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        
        var entity = await context.Set<RoleEntity>().FindAsync([id], cancellationToken);
        if (entity is null)
        {
            return false;
        }

        context.Set<RoleEntity>().Remove(entity);
        await context.SaveChangesWithExceptionHandlingAsync(cancellationToken);
        return true;
    }

    private static Role ToDomain(RoleEntity entity)
    {
        IEnumerable<Domain.Authorization.ValueObjects.ScopeTemplate>? scopeTemplates = null;
        
        if (!string.IsNullOrWhiteSpace(entity.ScopeTemplatesJson))
        {
            var dtos = JsonSerializer.Deserialize(entity.ScopeTemplatesJson, EFCoreServerIdentityJsonContext.Default.ListScopeTemplateDto);
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

    private static RoleEntity ToEntity(Role role)
    {
        string? scopeTemplatesJson = null;
        
        if (role.ScopeTemplates.Count > 0)
        {
            var dtos = role.ScopeTemplates.Select(ScopeTemplateDto.FromDomain).ToList();
            scopeTemplatesJson = JsonSerializer.Serialize(dtos, EFCoreServerIdentityJsonContext.Default.ListScopeTemplateDto);
        }

        return new RoleEntity
        {
            Id = role.Id,
            RevId = role.RevId,
            Code = role.Code,
            Name = role.Name,
            NormalizedName = role.NormalizedName,
            Description = role.Description,
            ScopeTemplatesJson = scopeTemplatesJson
        };
    }
}

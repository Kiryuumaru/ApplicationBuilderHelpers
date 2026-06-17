using System;
using System.Collections.Generic;
using System.Linq;
using Application.Server.Authorization.Interfaces.Inbound;
using Application.Server.Authorization.Interfaces.Outbound;
using Application.Server.Authorization.Models;
using Domain.Authorization.Entities;
using Domain.Authorization.Exceptions;
using Domain.Authorization.Models;
using Domain.Authorization.ValueObjects;
using Domain.Shared.Exceptions;
using RolesConstants = Domain.Authorization.Constants.Roles;

namespace Application.Server.Authorization.Services;

internal sealed class RoleService(IRoleRepository repository) : IRoleService
{

    public async Task<Role> CreateRoleAsync(RoleDescriptor descriptor, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        cancellationToken.ThrowIfCancellationRequested();

        var existing = await repository.GetByCodeAsync(descriptor.Code, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            throw new DuplicateEntityException("Role", descriptor.Code);
        }

        if (RolesConstants.TryGetByCode(descriptor.Code, out _))
        {
            throw new ReservedNameException(descriptor.Code, "roles");
        }

        var role = Role.Create(descriptor.Code, descriptor.Name, descriptor.Description, descriptor.IsSystemRole);
        role.ReplaceScopeTemplates(descriptor.ScopeTemplates ?? []);
        await repository.SaveAsync(role, cancellationToken).ConfigureAwait(false);
        return role;
    }

    public async Task<bool> DeleteRoleAsync(Guid roleId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (RolesConstants.IsStaticRole(roleId))
        {
            throw new SystemRoleException("Cannot delete a static role.", roleId);
        }

        var role = await repository.GetByIdAsync(roleId, cancellationToken).ConfigureAwait(false);
        if (role is null)
        {
            return false;
        }

        if (role.IsSystemRole)
        {
            throw new SystemRoleException("Cannot delete a system role.", roleId);
        }

        return await repository.DeleteAsync(roleId, cancellationToken).ConfigureAwait(false);
    }

    public Task<Role?> GetByCodeAsync(string code, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (RolesConstants.TryGetByCode(code, out var staticRole))
        {
            return Task.FromResult<Role?>(staticRole);
        }

        return repository.GetByCodeAsync(code, cancellationToken);
    }

    public Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (RolesConstants.TryGetById(id, out var staticRole))
        {
            return Task.FromResult<Role?>(staticRole);
        }

        return repository.GetByIdAsync(id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Role>> ListAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = new Dictionary<Guid, Role>();
        foreach (var role in RolesConstants.AllRoles)
        {
            result[role.Id] = role;
        }

        var dynamicRoles = await repository.ListAsync(cancellationToken).ConfigureAwait(false);
        foreach (var role in dynamicRoles)
        {
            if (RolesConstants.IsStaticRole(role.Id))
            {
                continue;
            }

            result[role.Id] = role;
        }

        return result.Values
            .OrderBy(static role => role.Code, StringComparer.Ordinal)
            .ToArray();
    }

    public async Task<Role> ReplaceScopeTemplatesAsync(Guid roleId, IEnumerable<ScopeTemplate> scopeTemplates, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scopeTemplates);
        cancellationToken.ThrowIfCancellationRequested();

        if (RolesConstants.IsStaticRole(roleId))
        {
            throw new SystemRoleException("Cannot modify scope templates of a static role.", roleId);
        }

        var role = await repository.GetByIdAsync(roleId, cancellationToken).ConfigureAwait(false) ?? throw new EntityNotFoundException("Role", roleId.ToString());
        if (role.IsSystemRole)
        {
            throw new SystemRoleException("Cannot modify scope templates of a system role.", roleId);
        }

        role.ReplaceScopeTemplates(scopeTemplates);
        await repository.SaveAsync(role, cancellationToken).ConfigureAwait(false);
        return role;
    }

    public async Task<Role> UpdateMetadataAsync(Guid roleId, string name, string? description, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (RolesConstants.IsStaticRole(roleId))
        {
            throw new SystemRoleException("Cannot modify metadata of a static role.", roleId);
        }

        var role = await repository.GetByIdAsync(roleId, cancellationToken).ConfigureAwait(false) ?? throw new EntityNotFoundException("Role", roleId.ToString());
        if (role.IsSystemRole)
        {
            throw new SystemRoleException("Cannot modify metadata of a system role.", roleId);
        }

        role.UpdateMetadata(name, description);
        
        await repository.SaveAsync(role, cancellationToken).ConfigureAwait(false);
        return role;
    }
}


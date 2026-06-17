using Application.Server.Authorization.Models;
using Domain.Authorization.Entities;
using Domain.Authorization.Models;
using Domain.Authorization.ValueObjects;

namespace Application.Server.Authorization.Interfaces.Inbound;

public interface IRoleService
{
    Task<Role> CreateRoleAsync(RoleDescriptor descriptor, CancellationToken cancellationToken);

    Task<bool> DeleteRoleAsync(Guid roleId, CancellationToken cancellationToken);

    Task<Role?> GetByCodeAsync(string code, CancellationToken cancellationToken);

    Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Role>> ListAsync(CancellationToken cancellationToken);

    Task<Role> ReplaceScopeTemplatesAsync(Guid roleId, IEnumerable<ScopeTemplate> scopeTemplates, CancellationToken cancellationToken);

    Task<Role> UpdateMetadataAsync(Guid roleId, string name, string? description, CancellationToken cancellationToken);
}

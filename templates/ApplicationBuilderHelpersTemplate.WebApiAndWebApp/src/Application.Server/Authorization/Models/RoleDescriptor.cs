using Domain.Authorization.ValueObjects;

namespace Application.Server.Authorization.Models;

public sealed record RoleDescriptor(
    string Code,
    string Name,
    string? Description,
    bool IsSystemRole,
    IReadOnlyCollection<ScopeTemplate>? ScopeTemplates = null);

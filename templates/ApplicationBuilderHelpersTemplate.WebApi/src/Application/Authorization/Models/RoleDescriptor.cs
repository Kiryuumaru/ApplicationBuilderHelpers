using Domain.Authorization.ValueObjects;

namespace Application.Authorization.Models;

/// <summary>
/// Descriptor for creating or updating a role.
/// </summary>
public sealed record RoleDescriptor(
    string Code,
    string Name,
    string? Description,
    bool IsSystemRole,
    IReadOnlyCollection<ScopeTemplate>? ScopeTemplates = null);

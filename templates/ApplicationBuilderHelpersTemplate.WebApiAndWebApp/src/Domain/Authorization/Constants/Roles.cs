using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Domain.Authorization.Entities;
using Domain.Authorization.Models;
using Domain.Authorization.ValueObjects;

namespace Domain.Authorization.Constants;

public static class Roles
{
    private static readonly IReadOnlyDictionary<Guid, RoleDefinition> DefinitionsById;
    private static readonly IReadOnlyDictionary<string, RoleDefinition> DefinitionsByCode;

    public static RoleDefinition Admin { get; }
    public static RoleDefinition User { get; }
    public static IReadOnlyList<RoleDefinition> All { get; }
    public static IReadOnlyCollection<Role> AllRoles => All.Select(static definition => definition.Instantiate()).ToArray();

    static Roles()
    {
        Admin = new RoleDefinition(
            Id: new Guid("00000000-0000-0000-0000-000000000001"),
            Code: "ADMIN",
            Name: "Administrator",
            Description: "Full access to all platform capabilities.",
            IsSystemRole: true,
            ScopeTemplates:
            [
                ScopeTemplate.Allow(Permissions.RootReadIdentifier),
                ScopeTemplate.Allow(Permissions.RootWriteIdentifier)
            ]);

        User = new RoleDefinition(
            Id: new Guid("00000000-0000-0000-0000-000000000002"),
            Code: "USER",
            Name: "User",
            Description: "Default role for authenticated users accessing their own data.",
            IsSystemRole: true,
            TemplateParametersOverride: ["roleUserId"],
            ScopeTemplates:
            [
                ScopeTemplate.Allow(Permissions.RootReadIdentifier, ("userId", "{roleUserId}")),
                ScopeTemplate.Allow(Permissions.RootWriteIdentifier, ("userId", "{roleUserId}"))
            ]);

        All = [Admin, User];
        DefinitionsById = All.ToDictionary(static definition => definition.Id);
        DefinitionsByCode = All.ToDictionary(static definition => definition.Code, StringComparer.OrdinalIgnoreCase);
    }

    public static bool TryGetById(Guid id, [NotNullWhen(true)] out Role? role)
    {
        if (DefinitionsById.TryGetValue(id, out var definition))
        {
            role = definition.Instantiate();
            return true;
        }

        role = null;
        return false;
    }

    public static bool TryGetByCode(string? code, [NotNullWhen(true)] out Role? role)
    {
        if (!string.IsNullOrWhiteSpace(code) && DefinitionsByCode.TryGetValue(code.Trim(), out var definition))
        {
            role = definition.Instantiate();
            return true;
        }

        role = null;
        return false;
    }

    public static bool IsStaticRole(Guid id) => DefinitionsById.ContainsKey(id);
}

using System;
using System.Collections.Generic;
using System.Linq;
using Domain.Authorization.Entities;
using Domain.Authorization.Extensions;
using Domain.Authorization.Models;
using Domain.Authorization.ValueObjects;

namespace Domain.Authorization.Constants;

public sealed record RoleDefinition(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    bool IsSystemRole,
    IReadOnlyList<ScopeTemplate> ScopeTemplates,
    IReadOnlyList<string>? TemplateParametersOverride = null)
{
    public IReadOnlyList<string> TemplateParameters { get; init; } = TemplateParametersOverride ?? Array.Empty<string>();

    public Role Instantiate()
    {
        var role = Role.Hydrate(Id, revId: null, Code, Name, Description, IsSystemRole);
        role.ReplaceScopeTemplates(ScopeTemplates);
        return role;
    }

    /// <summary>
    /// Expands all scope templates into scope directives using the provided parameter values.
    /// </summary>
    public IReadOnlyCollection<ScopeDirective> ExpandScope(IReadOnlyDictionary<string, string?> parameterValues)
    {
        return ScopeTemplates.ExpandToDirectives(parameterValues);
    }
}

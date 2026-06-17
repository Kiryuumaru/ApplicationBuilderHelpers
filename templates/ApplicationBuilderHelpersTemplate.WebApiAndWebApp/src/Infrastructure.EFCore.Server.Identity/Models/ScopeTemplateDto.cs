using Domain.Authorization.Enums;
using Domain.Authorization.ValueObjects;

namespace Infrastructure.EFCore.Server.Identity.Models;

internal sealed class ScopeTemplateDto
{
    public string Type { get; set; } = string.Empty;
    public string PermissionPath { get; set; } = string.Empty;
    public Dictionary<string, string>? Parameters { get; set; }

    public static ScopeTemplateDto FromDomain(ScopeTemplate template)
    {
        return new ScopeTemplateDto
        {
            Type = template.Type == ScopeDirectiveType.Allow ? "allow" : "deny",
            PermissionPath = template.PermissionPath,
            Parameters = template.ParameterTemplates.Count > 0
                ? template.ParameterTemplates.ToDictionary(p => p.Key, p => p.ValueTemplate)
                : null
        };
    }

    public ScopeTemplate ToDomain()
    {
        var type = string.Equals(Type, "allow", StringComparison.OrdinalIgnoreCase)
            ? ScopeDirectiveType.Allow
            : ScopeDirectiveType.Deny;

        var parameters = Parameters?
            .Select(kvp => (kvp.Key, kvp.Value))
            .ToArray() ?? [];

        return type == ScopeDirectiveType.Allow
            ? ScopeTemplate.Allow(PermissionPath, parameters)
            : ScopeTemplate.Deny(PermissionPath, parameters);
    }
}

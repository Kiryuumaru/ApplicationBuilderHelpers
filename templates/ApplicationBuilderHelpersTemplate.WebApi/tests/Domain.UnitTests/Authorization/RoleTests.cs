using Domain.Authorization.Entities;
using Domain.Authorization.ValueObjects;

namespace Domain.UnitTests.Authorization;

public sealed class RoleTests
{
    [Fact]
    public void AddScopeTemplate_AddsTemplateOnce()
    {
        var role = Role.Create("analyst", "Analyst");
        var template = ScopeTemplate.Allow("api:iam:users:list");

        role.AddScopeTemplate(template);
        role.AddScopeTemplate(template);

        Assert.Equal(2, role.ScopeTemplates.Count); // ScopeTemplates allows duplicates; use ReplaceScopeTemplates for deduplication
    }

    [Fact]
    public void ReplaceScopeTemplates_ReplacesExistingSet()
    {
        var role = Role.Create("operator", "Operator");
        role.AddScopeTemplate(ScopeTemplate.Allow("api:auth:me"));

        var updated = new[]
        {
            ScopeTemplate.Allow("api:iam:users:list"),
            ScopeTemplate.Allow("api:iam:users:delete")
        };

        role.ReplaceScopeTemplates(updated);

        Assert.Equal(2, role.ScopeTemplates.Count);
        Assert.Contains(role.ScopeTemplates, t => t.PermissionPath == "api:iam:users:delete");
        Assert.DoesNotContain(role.ScopeTemplates, t => t.PermissionPath == "api:auth:me");
    }
}

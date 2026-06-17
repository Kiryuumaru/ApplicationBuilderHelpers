using System.Collections.ObjectModel;
using Domain.Authorization.Extensions;
using Domain.Authorization.Models;
using Domain.Authorization.ValueObjects;
using Domain.Shared.Constants;
using Domain.Shared.Exceptions;
using Domain.Shared.Models;

namespace Domain.Authorization.Entities;

/// <summary>
/// Represents a security role that can be assigned to users.
/// </summary>
public sealed partial class Role : AggregateRoot
{
    private readonly List<ScopeTemplate> _scopeTemplates = new();

    public string Code { get; private set; }
    public string Name { get; private set; }
    public string NormalizedName { get; private set; }
    public string? Description { get; private set; }
    public bool IsSystemRole { get; private set; }

    public IReadOnlyCollection<ScopeTemplate> ScopeTemplates => _scopeTemplates.AsReadOnly();

    private Role(Guid id, string code, string name, string? description, bool isSystemRole) : base(id)
    {
        Code = NormalizeCode(code);
        Name = NormalizeName(name);
        NormalizedName = name.ToUpperInvariant();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        IsSystemRole = isSystemRole;
    }

    public static Role Create(string code, string name, string? description = null, bool isSystemRole = false)
        => new(Guid.NewGuid(), code, name, description, isSystemRole);

    /// <summary>
    /// Factory method for hydrating a Role from persistence. AOT-compatible.
    /// </summary>
    public static Role Hydrate(Guid id, Guid? revId, string code, string name, string? description, bool isSystemRole)
    {
        var role = new Role(id, code, name, description, isSystemRole);
        if (revId.HasValue)
        {
            role.RevId = revId.Value;
        }
        return role;
    }

    /// <summary>
    /// Factory method for hydrating a Role from persistence with scope templates. AOT-compatible.
    /// </summary>
    public static Role Hydrate(Guid id, Guid? revId, string code, string name, string? description, bool isSystemRole, IEnumerable<ScopeTemplate>? scopeTemplates)
    {
        var role = Hydrate(id, revId, code, name, description, isSystemRole);
        if (scopeTemplates is not null)
        {
            role._scopeTemplates.AddRange(scopeTemplates);
        }
        return role;
    }

    public void SetName(string name)
    {
        Name = NormalizeName(name);
        MarkAsModified();
    }

    public void SetNormalizedName(string normalizedName)
    {
        NormalizedName = normalizedName;
        MarkAsModified();
    }

    public void UpdateMetadata(string name, string? description)
    {
        Name = NormalizeName(name);
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        MarkAsModified();
    }

    public void PromoteToSystemRole()
    {
        if (!IsSystemRole)
        {
            IsSystemRole = true;
            MarkAsModified();
        }
    }

    /// <summary>
    /// Expands all scope templates into scope directives using the provided parameter values.
    /// </summary>
    public IReadOnlyCollection<ScopeDirective> ExpandScope(IReadOnlyDictionary<string, string?> parameterValues)
    {
        return _scopeTemplates.ExpandToDirectives(parameterValues);
    }

    /// <summary>
    /// Replaces all scope templates with the provided collection.
    /// </summary>
    public void ReplaceScopeTemplates(IEnumerable<ScopeTemplate> templates)
    {
        ArgumentNullException.ThrowIfNull(templates);

        _scopeTemplates.Clear();
        _scopeTemplates.AddRange(templates);
        MarkAsModified();
    }

    /// <summary>
    /// Adds a scope template to the role.
    /// </summary>
    public void AddScopeTemplate(ScopeTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);
        _scopeTemplates.Add(template);
        MarkAsModified();
    }

    private static string NormalizeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new DomainException("Role code cannot be null or empty.");
        }

        return code.Trim().ToUpperInvariant();
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Role name cannot be null or empty.");
        }

        return name.Trim();
    }

}

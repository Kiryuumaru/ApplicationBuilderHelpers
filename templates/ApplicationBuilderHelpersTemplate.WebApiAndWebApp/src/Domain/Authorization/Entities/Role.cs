using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Domain.Authorization.Extensions;
using Domain.Authorization.ValueObjects;
using Domain.Shared.Constants;
using Domain.Shared.Exceptions;
using Domain.Shared.Models;

namespace Domain.Authorization.Entities;

public class Role : AggregateRoot
{
    private readonly List<ScopeTemplate> _scopeTemplates = new();

    public string Code { get; private set; }
    public string Name { get; private set; }
    public string NormalizedName { get; private set; }
    public string? Description { get; private set; }
    public bool IsSystemRole { get; private set; }

    public IReadOnlyCollection<ScopeTemplate> ScopeTemplates => _scopeTemplates.AsReadOnly();

    protected Role(Guid id, string code, string name, string? description, bool isSystemRole) : base(id)
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

    #region Role Claim Parsing

    /// <summary>
    /// Represents a parsed role claim in the format "CODE;param1=value1;param2=value2".
    /// </summary>
    /// <param name="Original">The original claim string.</param>
    /// <param name="Code">The role code (uppercase, normalized).</param>
    /// <param name="Parameters">The parameter key-value pairs.</param>
    public readonly record struct ParsedRoleClaim(
        string Original,
        string Code,
        IReadOnlyDictionary<string, string> Parameters)
    {
        /// <summary>
        /// Gets whether this role claim has any parameters.
        /// </summary>
        public bool HasParameters => Parameters.Count > 0;

        /// <summary>
        /// Formats the role claim back to string: "CODE;param1=value1;param2=value2".
        /// </summary>
        public override string ToString()
        {
            if (Parameters.Count == 0)
            {
                return Code;
            }

            var parts = new List<string>(Parameters.Count + 1) { Code };
            foreach (var (key, value) in Parameters.OrderBy(p => p.Key, StringComparer.Ordinal))
            {
                parts.Add($"{key}={value}");
            }

            return string.Join(';', parts);
        }
    }

    /// <summary>
    /// Parses a role claim string in the format "CODE;param1=value1;param2=value2".
    /// </summary>
    /// <param name="claim">The role claim string to parse.</param>
    /// <returns>A parsed role claim with code and parameters.</returns>
    /// <exception cref="ArgumentNullException">Thrown when claim is null.</exception>
    /// <exception cref="FormatException">Thrown when the claim format is invalid.</exception>
    public static ParsedRoleClaim ParseRoleClaim(string claim)
    {
        ArgumentNullException.ThrowIfNull(claim);

        var trimmed = claim.Trim();
        if (trimmed.Length == 0)
        {
            throw new FormatException("Role claim cannot be empty.");
        }

        // Check for control characters (security)
        foreach (var c in trimmed)
        {
            if (char.IsControl(c))
            {
                throw new FormatException("Role claim cannot contain control characters.");
            }
        }

        // Format: "CODE;param1=value1;param2=value2" or just "CODE"
        var semicolonIndex = trimmed.IndexOf(';');
        if (semicolonIndex < 0)
        {
            // No parameters - just the code
            var code = NormalizeCode(trimmed);
            return new ParsedRoleClaim(claim, code, EmptyCollections.StringStringDictionary);
        }

        if (semicolonIndex == 0)
        {
            throw new FormatException("Role claim cannot start with a semicolon.");
        }

        // Extract and normalize the code
        var codeStr = trimmed[..semicolonIndex].Trim();
        if (codeStr.Length == 0)
        {
            throw new FormatException("Role code cannot be empty.");
        }

        var normalizedCode = NormalizeCode(codeStr);

        // Parse parameters from remaining semicolon-separated parts
        var paramPart = trimmed[(semicolonIndex + 1)..];
        if (paramPart.Length == 0)
        {
            // Trailing semicolon with no params - treat as no params
            return new ParsedRoleClaim(claim, normalizedCode, EmptyCollections.StringStringDictionary);
        }

        var parameters = new Dictionary<string, string>(StringComparer.Ordinal);
        var assignments = paramPart.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var assignment in assignments)
        {
            var equalsIndex = assignment.IndexOf('=');
            if (equalsIndex < 0)
            {
                throw new FormatException($"Role claim parameter '{assignment}' must use the 'name=value' format.");
            }

            var name = assignment[..equalsIndex].Trim();
            var value = assignment[(equalsIndex + 1)..].Trim();

            if (name.Length == 0)
            {
                throw new FormatException("Role claim parameter names cannot be empty.");
            }

            if (value.Length == 0)
            {
                throw new FormatException($"Role claim parameter '{name}' requires a value.");
            }

            // Last value wins for duplicate keys
            parameters[name] = value;
        }

        return new ParsedRoleClaim(
            claim,
            normalizedCode,
            parameters.Count == 0 ? EmptyCollections.StringStringDictionary : new ReadOnlyDictionary<string, string>(parameters));
    }

    /// <summary>
    /// Attempts to parse a role claim string without throwing exceptions.
    /// </summary>
    /// <param name="claim">The role claim string to parse.</param>
    /// <param name="parsed">When successful, contains the parsed role claim.</param>
    /// <returns><c>true</c> if parsing succeeded; otherwise <c>false</c>.</returns>
    public static bool TryParseRoleClaim(string claim, out ParsedRoleClaim parsed)
    {
        try
        {
            parsed = ParseRoleClaim(claim);
            return true;
        }
        catch
        {
            parsed = new ParsedRoleClaim(string.Empty, string.Empty, EmptyCollections.StringStringDictionary);
            return false;
        }
    }

    /// <summary>
    /// Formats a role code and parameters into a role claim string.
    /// </summary>
    /// <param name="code">The role code.</param>
    /// <param name="parameters">Optional parameter values.</param>
    /// <returns>A formatted role claim string like "CODE;param1=value1;param2=value2".</returns>
    /// <exception cref="ArgumentException">Thrown when code is null or empty.</exception>
    public static string FormatRoleClaim(string code, IReadOnlyDictionary<string, string?>? parameters)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        var normalizedCode = NormalizeCode(code);

        if (parameters is null || parameters.Count == 0)
        {
            return normalizedCode;
        }

        var parts = new List<string>(parameters.Count + 1) { normalizedCode };

        foreach (var (key, value) in parameters.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
            {
                parts.Add($"{key}={value}");
            }
        }

        return string.Join(';', parts);
    }

    #endregion
}

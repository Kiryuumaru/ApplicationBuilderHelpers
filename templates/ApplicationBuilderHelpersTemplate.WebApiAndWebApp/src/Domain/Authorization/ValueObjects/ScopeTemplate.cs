using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Domain.Authorization.Enums;
using Domain.Shared.Exceptions;
using Domain.Shared.Models;

namespace Domain.Authorization.ValueObjects;

/// <summary>
/// A template for generating scope directives from role definitions.
/// Supports parameter placeholders that are expanded at runtime.
/// </summary>
public class ScopeTemplate : ValueObject
{
    private static readonly Regex PlaceholderRegex = new("\\{([a-zA-Z0-9_]+)\\}", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly IReadOnlyCollection<(string Key, string ValueTemplate)> EmptyParameterTemplates =
        Array.Empty<(string, string)>();

    /// <summary>
    /// Gets the directive type (Allow or Deny).
    /// </summary>
    public ScopeDirectiveType Type { get; }

    /// <summary>
    /// Gets the permission path (e.g., "_read", "api:user:profile:_read").
    /// </summary>
    public string PermissionPath { get; }

    /// <summary>
    /// Gets the parameter templates as key-value pairs where values may contain placeholders like {roleUserId}.
    /// </summary>
    public IReadOnlyCollection<(string Key, string ValueTemplate)> ParameterTemplates { get; }

    /// <summary>
    /// Gets the names of required template parameters (extracted from placeholders).
    /// </summary>
    public IReadOnlyCollection<string> RequiredParameters { get; }

    protected ScopeTemplate(
        ScopeDirectiveType type,
        string permissionPath,
        IReadOnlyCollection<(string Key, string ValueTemplate)> parameterTemplates,
        IReadOnlyCollection<string> requiredParameters)
    {
        Type = type;
        PermissionPath = permissionPath;
        ParameterTemplates = parameterTemplates;
        RequiredParameters = requiredParameters;
    }

    /// <summary>
    /// Creates an allow scope template.
    /// </summary>
    /// <param name="permissionPath">The permission path.</param>
    /// <param name="parameters">Parameter templates as (key, valueTemplate) tuples.</param>
    /// <returns>A new scope template.</returns>
    public static ScopeTemplate Allow(string permissionPath, params (string Key, string ValueTemplate)[] parameters)
    {
        return Create(ScopeDirectiveType.Allow, permissionPath, parameters);
    }

    /// <summary>
    /// Creates a deny scope template.
    /// </summary>
    /// <param name="permissionPath">The permission path.</param>
    /// <param name="parameters">Parameter templates as (key, valueTemplate) tuples.</param>
    /// <returns>A new scope template.</returns>
    public static ScopeTemplate Deny(string permissionPath, params (string Key, string ValueTemplate)[] parameters)
    {
        return Create(ScopeDirectiveType.Deny, permissionPath, parameters);
    }

    /// <summary>
    /// Creates a scope template with the specified type.
    /// </summary>
    private static ScopeTemplate Create(
        ScopeDirectiveType type,
        string permissionPath,
        (string Key, string ValueTemplate)[] parameters)
    {
        if (string.IsNullOrWhiteSpace(permissionPath))
        {
            throw new ArgumentException("Permission path cannot be null or empty.", nameof(permissionPath));
        }

        var normalizedPath = permissionPath.Trim();

        IReadOnlyCollection<(string Key, string ValueTemplate)> paramTemplates;
        if (parameters.Length == 0)
        {
            paramTemplates = EmptyParameterTemplates;
        }
        else
        {
            var list = new List<(string, string)>(parameters.Length);
            foreach (var (key, valueTemplate) in parameters)
            {
                if (string.IsNullOrEmpty(key))
                {
                    throw new ArgumentException("Parameter key cannot be null or empty.");
                }
                list.Add((key.Trim(), valueTemplate ?? string.Empty));
            }
            paramTemplates = list.AsReadOnly();
        }

        var requiredParams = ExtractRequiredParameters(paramTemplates);

        return new ScopeTemplate(type, normalizedPath, paramTemplates, requiredParams);
    }

    /// <summary>
    /// Indicates whether this template requires parameters to expand.
    /// </summary>
    public bool RequiresParameters => RequiredParameters.Count > 0;

    /// <summary>
    /// Expands this template into a <see cref="ScopeDirective"/> using the provided parameter values.
    /// </summary>
    /// <param name="parameterValues">Values for template placeholders (e.g., roleUserId → "abc123").</param>
    /// <returns>A fully expanded scope directive.</returns>
    /// <exception cref="DomainException">Thrown when required parameters are missing.</exception>
    public ScopeDirective Expand(IReadOnlyDictionary<string, string?>? parameterValues)
    {
        if (RequiresParameters && (parameterValues is null || parameterValues.Count == 0))
        {
            throw new DomainException($"Scope template requires parameters [{string.Join(", ", RequiredParameters)}].");
        }

        var expandedParams = new Dictionary<string, string>(ParameterTemplates.Count, StringComparer.Ordinal);

        foreach (var (key, valueTemplate) in ParameterTemplates)
        {
            var expandedValue = ExpandPlaceholders(valueTemplate, parameterValues);
            expandedParams[key] = expandedValue;
        }

        return Type == ScopeDirectiveType.Allow
            ? ScopeDirective.Allow(PermissionPath, expandedParams.AsReadOnly())
            : ScopeDirective.Deny(PermissionPath, expandedParams.AsReadOnly());
    }

    /// <summary>
    /// Expands this template into multiple directives using multiple parameter sets.
    /// </summary>
    public IEnumerable<ScopeDirective> ExpandMany(IEnumerable<IReadOnlyDictionary<string, string?>> parameterSets)
    {
        ArgumentNullException.ThrowIfNull(parameterSets);
        foreach (var parameters in parameterSets)
        {
            yield return Expand(parameters);
        }
    }

    /// <summary>
    /// Returns the string representation of this template (for display/debugging).
    /// </summary>
    public override string ToString()
    {
        var typeName = Type == ScopeDirectiveType.Allow ? "Allow" : "Deny";

        if (ParameterTemplates.Count == 0)
        {
            return $"{typeName}({PermissionPath})";
        }

        var paramStr = string.Join(", ", ParameterTemplates.Select(p => $"{p.Key}={p.ValueTemplate}"));
        return $"{typeName}({PermissionPath}; {paramStr})";
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Type;
        yield return PermissionPath;
        foreach (var (key, valueTemplate) in ParameterTemplates.OrderBy(static p => p.Key, StringComparer.Ordinal))
        {
            yield return key;
            yield return valueTemplate;
        }
    }

    private static IReadOnlyCollection<string> ExtractRequiredParameters(
        IReadOnlyCollection<(string Key, string ValueTemplate)> parameterTemplates)
    {
        var placeholders = new HashSet<string>(StringComparer.Ordinal);

        foreach (var (_, valueTemplate) in parameterTemplates)
        {
            var matches = PlaceholderRegex.Matches(valueTemplate);
            foreach (Match match in matches)
            {
                placeholders.Add(match.Groups[1].Value);
            }
        }

        return placeholders.Count == 0
            ? Array.Empty<string>()
            : placeholders.OrderBy(static p => p, StringComparer.Ordinal).ToArray();
    }

    private string ExpandPlaceholders(string template, IReadOnlyDictionary<string, string?>? parameterValues)
    {
        if (string.IsNullOrEmpty(template) || parameterValues is null)
        {
            return template;
        }

        var result = template;

        var matches = PlaceholderRegex.Matches(template);
        foreach (Match match in matches)
        {
            var paramName = match.Groups[1].Value;
            if (!parameterValues.TryGetValue(paramName, out var value) || value is null)
            {
                throw new DomainException($"Missing value for scope template parameter '{paramName}'.");
            }

            result = result.Replace(match.Value, value, StringComparison.Ordinal);
        }

        // Validate no unresolved placeholders remain
        if (result.Contains('{') && result.Contains('}'))
        {
            throw new DomainException($"Scope template contains unresolved parameters after expansion.");
        }

        return result;
    }
}

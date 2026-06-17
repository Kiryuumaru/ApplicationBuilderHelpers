using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Domain.Authorization.Enums;
using Domain.Shared.Constants;
using Domain.Shared.Models;

namespace Domain.Authorization.ValueObjects;

/// <summary>
/// Represents a single scope directive that grants or revokes permission to a path.
/// Format: "&lt;type&gt;;&lt;permission_path&gt;[;&lt;param1&gt;=&lt;value1&gt;[;&lt;param2&gt;=&lt;value2&gt;...]]"
/// </summary>
public class ScopeDirective : ValueObject
{
    private static readonly char[] Separators = [';'];

    /// <summary>
    /// Gets the directive type (Allow or Deny).
    /// </summary>
    public ScopeDirectiveType Type { get; }

    /// <summary>
    /// Gets the permission path (e.g., "api:user:profile:_read").
    /// </summary>
    public string PermissionPath { get; }

    /// <summary>
    /// Gets the parameters associated with this directive (e.g., userId=abc123).
    /// </summary>
    public IReadOnlyDictionary<string, string> Parameters { get; }

    protected ScopeDirective(ScopeDirectiveType type, string permissionPath, IReadOnlyDictionary<string, string> parameters)
    {
        Type = type;
        PermissionPath = permissionPath;
        Parameters = parameters;
    }

    /// <summary>
    /// Creates an allow directive for the specified permission path.
    /// </summary>
    /// <param name="permissionPath">The permission path to allow.</param>
    /// <param name="parameters">Optional parameters to scope the permission.</param>
    /// <returns>A new allow directive.</returns>
    public static ScopeDirective Allow(string permissionPath, IReadOnlyDictionary<string, string>? parameters = null)
    {
        ValidatePermissionPath(permissionPath);
        return new ScopeDirective(
            ScopeDirectiveType.Allow,
            permissionPath.Trim(),
            parameters ?? EmptyCollections.StringStringDictionary);
    }

    /// <summary>
    /// Creates an allow directive for the specified permission path with parameters.
    /// </summary>
    /// <param name="permissionPath">The permission path to allow.</param>
    /// <param name="parameters">Parameters as key-value tuples.</param>
    /// <returns>A new allow directive.</returns>
    public static ScopeDirective Allow(string permissionPath, params (string Key, string Value)[] parameters)
    {
        ValidatePermissionPath(permissionPath);
        var paramDict = BuildParameterDictionary(parameters);
        return new ScopeDirective(ScopeDirectiveType.Allow, permissionPath.Trim(), paramDict);
    }

    /// <summary>
    /// Creates a deny directive for the specified permission path.
    /// </summary>
    /// <param name="permissionPath">The permission path to deny.</param>
    /// <param name="parameters">Optional parameters to scope the denial.</param>
    /// <returns>A new deny directive.</returns>
    public static ScopeDirective Deny(string permissionPath, IReadOnlyDictionary<string, string>? parameters = null)
    {
        ValidatePermissionPath(permissionPath);
        return new ScopeDirective(
            ScopeDirectiveType.Deny,
            permissionPath.Trim(),
            parameters ?? EmptyCollections.StringStringDictionary);
    }

    /// <summary>
    /// Creates a deny directive for the specified permission path with parameters.
    /// </summary>
    /// <param name="permissionPath">The permission path to deny.</param>
    /// <param name="parameters">Parameters as key-value tuples.</param>
    /// <returns>A new deny directive.</returns>
    public static ScopeDirective Deny(string permissionPath, params (string Key, string Value)[] parameters)
    {
        ValidatePermissionPath(permissionPath);
        var paramDict = BuildParameterDictionary(parameters);
        return new ScopeDirective(ScopeDirectiveType.Deny, permissionPath.Trim(), paramDict);
    }

    /// <summary>
    /// Parses a scope directive from its string representation.
    /// Format: "allow;path" or "allow;path;key=value;key2=value2" or "deny;path;..."
    /// </summary>
    /// <param name="directive">The directive string to parse.</param>
    /// <returns>The parsed scope directive.</returns>
    /// <exception cref="FormatException">Thrown when the directive format is invalid.</exception>
    public static ScopeDirective Parse(string directive)
    {
        if (!TryParse(directive, out var result, out var error))
        {
            throw new FormatException(error);
        }
        return result;
    }

    /// <summary>
    /// Attempts to parse a scope directive from its string representation.
    /// </summary>
    /// <param name="directive">The directive string to parse.</param>
    /// <param name="result">The parsed directive if successful.</param>
    /// <returns>True if parsing succeeded; otherwise false.</returns>
    public static bool TryParse(string? directive, [NotNullWhen(true)] out ScopeDirective? result)
    {
        return TryParse(directive, out result, out _);
    }

    /// <summary>
    /// Attempts to parse a scope directive from its string representation.
    /// </summary>
    /// <param name="directive">The directive string to parse.</param>
    /// <param name="result">The parsed directive if successful.</param>
    /// <param name="error">The error message if parsing failed.</param>
    /// <returns>True if parsing succeeded; otherwise false.</returns>
    public static bool TryParse(string? directive, [NotNullWhen(true)] out ScopeDirective? result, out string? error)
    {
        result = null;
        error = null;

        if (string.IsNullOrWhiteSpace(directive))
        {
            error = "Directive cannot be null or empty.";
            return false;
        }

        var parts = directive.Split(Separators, StringSplitOptions.None);
        if (parts.Length < 2)
        {
            error = "Directive must have at least type and permission path.";
            return false;
        }

        var typePart = parts[0].Trim().ToLowerInvariant();
        ScopeDirectiveType type;
        if (string.Equals(typePart, "allow", StringComparison.Ordinal))
        {
            type = ScopeDirectiveType.Allow;
        }
        else if (string.Equals(typePart, "deny", StringComparison.Ordinal))
        {
            type = ScopeDirectiveType.Deny;
        }
        else
        {
            error = $"Unknown directive type '{parts[0]}'. Expected 'allow' or 'deny'.";
            return false;
        }

        var permissionPath = parts[1].Trim();
        if (string.IsNullOrEmpty(permissionPath))
        {
            error = "Permission path cannot be empty.";
            return false;
        }

        Dictionary<string, string>? parameters = null;
        for (var i = 2; i < parts.Length; i++)
        {
            var paramPart = parts[i].Trim();
            if (string.IsNullOrEmpty(paramPart))
            {
                continue;
            }

            var equalsIndex = paramPart.IndexOf('=');
            if (equalsIndex <= 0)
            {
                error = $"Invalid parameter format '{paramPart}'. Expected 'key=value'.";
                return false;
            }

            var key = paramPart[..equalsIndex].Trim();
            var value = paramPart[(equalsIndex + 1)..].Trim();

            if (string.IsNullOrEmpty(key))
            {
                error = "Parameter key cannot be empty.";
                return false;
            }

            parameters ??= new Dictionary<string, string>(StringComparer.Ordinal);
            parameters[key] = value;
        }

        result = new ScopeDirective(type, permissionPath, parameters?.AsReadOnly() ?? EmptyCollections.StringStringDictionary);
        return true;
    }

    /// <summary>
    /// Converts the directive to its string representation.
    /// </summary>
    /// <returns>The directive string in format "type;path[;key=value...]".</returns>
    public override string ToString()
    {
        var typeName = Type == ScopeDirectiveType.Allow ? "allow" : "deny";

        if (Parameters.Count == 0)
        {
            return $"{typeName};{PermissionPath}";
        }

        var paramParts = Parameters
            .OrderBy(static kv => kv.Key, StringComparer.Ordinal)
            .Select(static kv => $"{kv.Key}={kv.Value}");

        return $"{typeName};{PermissionPath};{string.Join(";", paramParts)}";
    }

    /// <summary>
    /// Converts this Allow directive to a permission identifier string.
    /// Format: "path" or "path;key=value;key2=value2"
    /// </summary>
    /// <returns>The permission identifier if this is an Allow directive; null for Deny directives.</returns>
    public string? ToPermissionIdentifier()
    {
        if (Type != ScopeDirectiveType.Allow)
        {
            return null;
        }

        if (Parameters.Count == 0)
        {
            return PermissionPath;
        }

        var paramParts = Parameters
            .OrderBy(static kv => kv.Key, StringComparer.Ordinal)
            .Select(static kv => $"{kv.Key}={kv.Value}");

        return $"{PermissionPath};{string.Join(";", paramParts)}";
    }

    /// <summary>
    /// Creates a new directive with an additional parameter.
    /// </summary>
    /// <param name="key">The parameter key.</param>
    /// <param name="value">The parameter value.</param>
    /// <returns>A new directive with the added parameter.</returns>
    public ScopeDirective WithParameter(string key, string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(value);

        var newParams = new Dictionary<string, string>(Parameters, StringComparer.Ordinal)
        {
            [key.Trim()] = value
        };

        return new ScopeDirective(Type, PermissionPath, newParams.AsReadOnly());
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Type;
        yield return PermissionPath;
        foreach (var kvp in Parameters.OrderBy(static kv => kv.Key, StringComparer.Ordinal))
        {
            yield return kvp.Key;
            yield return kvp.Value;
        }
    }

    private static void ValidatePermissionPath(string permissionPath)
    {
        if (string.IsNullOrWhiteSpace(permissionPath))
        {
            throw new ArgumentException("Permission path cannot be null or empty.", nameof(permissionPath));
        }
    }

    private static IReadOnlyDictionary<string, string> BuildParameterDictionary((string Key, string Value)[] parameters)
    {
        if (parameters.Length == 0)
        {
            return EmptyCollections.StringStringDictionary;
        }

        var dict = new Dictionary<string, string>(parameters.Length, StringComparer.Ordinal);
        foreach (var (key, value) in parameters)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Parameter key cannot be null or empty.");
            }
            dict[key.Trim()] = value ?? string.Empty;
        }

        return dict.AsReadOnly();
    }
}

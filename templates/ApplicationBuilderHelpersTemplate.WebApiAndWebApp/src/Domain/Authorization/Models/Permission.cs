using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Domain.Authorization.Enums;
using Domain.Shared.Constants;

namespace Domain.Authorization.Models;

public sealed class Permission
{
    public required string Identifier { get; init; }

    public required string Description { get; init; }

    public required Permission[] Permissions { get; init; }

    public required string[] Parameters { get; init; }

    public PermissionAccessCategory AccessCategory { get; init; } = PermissionAccessCategory.Unspecified;

    public Permission? Parent { get; private set; }

    public bool HasChildren => Permissions.Length > 0;

    public bool IsRead => AccessCategory == PermissionAccessCategory.Read;

    public bool IsWrite => AccessCategory == PermissionAccessCategory.Write;

    public string Path => _cachedPath ??= Parent == null ? Identifier : $"{Parent.Path}:{Identifier}";

    private string? _cachedPath;
    private string[]? _cachedParameterHierarchy;

    internal void SetParent(Permission? parent)
    {
        Parent = parent;
        _cachedPath = null;
        _cachedParameterHierarchy = null;

        foreach (var child in Permissions)
        {
            child.SetParent(this);
        }
    }

    public string BuildPath(IReadOnlyDictionary<string, string?>? parameterValues = null)
    {
        var segments = new Stack<string>();
        var current = this;

        while (current is not null)
        {
            if (current.Parameters.Length > 0)
            {
                var parameterSegment = BuildParameterSegment(current.Parameters, parameterValues);
                if (parameterSegment is not null)
                {
                    segments.Push(parameterSegment);
                }
            }

            segments.Push(current.Identifier);

            current = current.Parent;
        }

        return string.Join(':', segments);
    }

    public IReadOnlyCollection<string> GetParameterHierarchy()
    {
        if (_cachedParameterHierarchy is not null)
        {
            return _cachedParameterHierarchy;
        }

        var stack = new Stack<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var current = this;

        while (current is not null)
        {
            for (var index = current.Parameters.Length - 1; index >= 0; index--)
            {
                var name = current.Parameters[index];
                if (seen.Add(name))
                {
                    stack.Push(name);
                }
            }

            current = current.Parent;
        }

        _cachedParameterHierarchy = stack.Count == 0
            ? []
            : [.. stack];

        return _cachedParameterHierarchy;
    }

    public IEnumerable<Permission> Traverse()
    {
        yield return this;

        foreach (var child in Permissions)
        {
            foreach (var descendant in child.Traverse())
            {
                yield return descendant;
            }
        }
    }

    public static ParsedIdentifier ParseIdentifier(string identifier)
    {
        ArgumentException.ThrowIfNullOrEmpty(identifier);

        var trimmed = identifier.Trim();
        if (trimmed.Length == 0)
        {
            throw new FormatException("Permission identifier cannot be empty.");
        }

        // Format: "path;key=value;key2=value2" or just "path" (no parameters)
        var semicolonIndex = trimmed.IndexOf(';');
        if (semicolonIndex > 0)
        {
            return ParseSemicolonFormat(trimmed, semicolonIndex);
        }

        // No semicolon - treat as path-only (no parameters)
        return ParsePathOnly(trimmed);
    }

    private static ParsedIdentifier ParseSemicolonFormat(string identifier, int firstSemicolonIndex)
    {
        // Format: "api:user:profile:read;userId=abc;accountId=xyz"
        // Everything before first semicolon is the path
        // Everything after is key=value pairs separated by semicolons

        var path = identifier[..firstSemicolonIndex].Trim();
        if (path.Length == 0)
        {
            throw new FormatException("Permission identifier path cannot be empty.");
        }

        // Validate path segments (should be colon-separated)
        var pathSegments = path.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (pathSegments.Length == 0)
        {
            throw new FormatException("Permission identifier must contain at least one path segment.");
        }

        var canonical = string.Join(':', pathSegments);

        // Parse parameters from remaining semicolon-separated parts
        Dictionary<string, string>? parameters = null;
        var paramPart = identifier[(firstSemicolonIndex + 1)..];

        if (paramPart.Length > 0)
        {
            parameters = new Dictionary<string, string>(StringComparer.Ordinal);
            var assignments = paramPart.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var assignment in assignments)
            {
                var parts = assignment.Split('=', 2, StringSplitOptions.TrimEntries);
                if (parts.Length != 2)
                {
                    throw new FormatException("Permission identifier parameter segments must use the 'name=value' format.");
                }

                var name = parts[0];
                var value = parts[1];

                if (name.Length == 0)
                {
                    throw new FormatException("Permission identifier parameter names cannot be empty.");
                }

                if (value.Length == 0)
                {
                    throw new FormatException($"Permission identifier parameter '{name}' requires a value.");
                }

                parameters[name] = value;
            }
        }

        return new ParsedIdentifier(identifier, canonical, parameters is null ? EmptyCollections.StringStringDictionary : new ReadOnlyDictionary<string, string>(parameters));
    }

    private static ParsedIdentifier ParsePathOnly(string trimmed)
    {
        // Simple path without parameters: "api:user:profile:read"
        var segments = trimmed.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        
        if (segments.Length == 0)
        {
            throw new FormatException("Permission identifier must contain at least one path segment.");
        }

        var canonical = string.Join(':', segments);
        return new ParsedIdentifier(trimmed, canonical, EmptyCollections.StringStringDictionary);
    }

    public static bool TryParseIdentifier(string identifier, out ParsedIdentifier parsed)
    {
        try
        {
            parsed = ParseIdentifier(identifier);
            return true;
        }
        catch
        {
            parsed = new ParsedIdentifier(string.Empty, string.Empty, EmptyCollections.StringStringDictionary);
            return false;
        }
    }

    public static string NormalizeIdentifier(string identifier)
    {
        var parsed = ParseIdentifier(identifier);
        return parsed.Canonical;
    }

    private static string? BuildParameterSegment(string[] parameterNames, IReadOnlyDictionary<string, string?>? providedValues)
    {
        if (providedValues is null || providedValues.Count == 0)
        {
            return null;
        }

        var assignments = new List<string>(parameterNames.Length);
        foreach (var name in parameterNames)
        {
            if (providedValues.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                assignments.Add($"{name}={value}");
            }
        }

        return assignments.Count == 0 ? null : $"[{string.Join(';', assignments)}]";
    }

    public readonly record struct ParsedIdentifier(string Identifier, string Canonical, IReadOnlyDictionary<string, string> Parameters)
    {
        public bool HasParameters => Parameters.Count > 0;
    }
}

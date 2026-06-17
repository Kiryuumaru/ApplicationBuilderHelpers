using System;
using System.Collections.Generic;
using System.Linq;
using Domain.Authorization.Constants;
using Domain.Authorization.Entities;
using Domain.Authorization.Enums;
using Domain.Authorization.Models;
using Domain.Authorization.ValueObjects;

namespace Domain.Authorization.Services;

/// <summary>
/// Evaluates whether a scope grants a requested permission.
/// </summary>
public static class ScopeEvaluator
{
    /// <summary>
    /// Gets all Read-leaf permission paths.
    /// </summary>
    public static IReadOnlyCollection<string> GetRLeafPermissions() => PermissionCache.ReadLeafPaths;

    /// <summary>
    /// Gets all Write-leaf permission paths.
    /// </summary>
    public static IReadOnlyCollection<string> GetWLeafPermissions() => PermissionCache.WriteLeafPaths;

    /// <summary>
    /// Determines whether the given scope grants access to the requested permission.
    /// </summary>
    /// <param name="scope">The collection of scope directives.</param>
    /// <param name="permissionPath">The permission path being requested.</param>
    /// <param name="requestParameters">Parameters from the request context (e.g., userId, accountId).</param>
    /// <returns>True if access is granted; otherwise false.</returns>
    public static bool HasPermission(
        IEnumerable<ScopeDirective>? scope,
        string permissionPath,
        IReadOnlyDictionary<string, string>? requestParameters = null)
    {
        if (string.IsNullOrWhiteSpace(permissionPath))
        {
            return false;
        }

        var directives = scope?.ToList() ?? [];

        var allows = directives.Where(static d => d.Type == ScopeDirectiveType.Allow).ToList();
        var denies = directives.Where(static d => d.Type == ScopeDirectiveType.Deny).ToList();

        // EMPTY CHECK: If no allows AND no denies → DENY (authenticated but permissionless)
        if (allows.Count == 0 && denies.Count == 0)
        {
            return false;
        }

        // DENY-ONLY MODE: If no allows exist, allow all except denied
        if (allows.Count == 0)
        {
            // Check if ANY deny matches the request
            foreach (var deny in denies)
            {
                if (DirectiveMatches(deny, permissionPath, requestParameters))
                {
                    return false;
                }
            }
            // No deny matches → ALLOW
            return true;
        }

        // ALLOW EVALUATION: Check if ANY allow directive matches the request
        var hasAllowMatch = false;
        foreach (var allow in allows)
        {
            if (DirectiveMatches(allow, permissionPath, requestParameters))
            {
                hasAllowMatch = true;
                break;
            }
        }

        if (!hasAllowMatch)
        {
            return false;
        }

        // DENY EVALUATION: Check if ANY deny directive matches the request
        foreach (var deny in denies)
        {
            if (DirectiveMatches(deny, permissionPath, requestParameters))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Determines whether the given scope grants access to any of the requested permissions.
    /// </summary>
    public static bool HasAnyPermission(
        IEnumerable<ScopeDirective>? scope,
        IEnumerable<string> permissionPaths,
        IReadOnlyDictionary<string, string>? requestParameters = null)
    {
        if (permissionPaths is null)
        {
            return false;
        }

        var directiveList = scope?.ToList();

        foreach (var path in permissionPaths)
        {
            if (HasPermission(directiveList, path, requestParameters))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Determines whether the given scope grants access to all of the requested permissions.
    /// </summary>
    public static bool HasAllPermissions(
        IEnumerable<ScopeDirective>? scope,
        IEnumerable<string> permissionPaths,
        IReadOnlyDictionary<string, string>? requestParameters = null)
    {
        if (permissionPaths is null)
        {
            return false;
        }

        var paths = permissionPaths.Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
        if (paths.Count == 0)
        {
            return false;
        }

        var directiveList = scope?.ToList();

        foreach (var path in paths)
        {
            if (!HasPermission(directiveList, path, requestParameters))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Extracts parameters from matching allow directives for a given permission path.
    /// </summary>
    public static IReadOnlyDictionary<string, string> GetParameters(
        IEnumerable<ScopeDirective>? scope,
        string permissionPath)
    {
        if (string.IsNullOrWhiteSpace(permissionPath) || scope is null)
        {
            return new Dictionary<string, string>(0, StringComparer.Ordinal);
        }

        var result = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var directive in scope.Where(static d => d.Type == ScopeDirectiveType.Allow))
        {
            if (PathMatches(directive.PermissionPath, permissionPath))
            {
                foreach (var kvp in directive.Parameters)
                {
                    result.TryAdd(kvp.Key, kvp.Value);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Checks if a directive matches the requested permission and parameters.
    /// </summary>
    private static bool DirectiveMatches(
        ScopeDirective directive,
        string requestedPath,
        IReadOnlyDictionary<string, string>? requestParameters)
    {
        // 1. Check if permission path matches
        if (!PathMatches(directive.PermissionPath, requestedPath))
        {
            return false;
        }

        // 2. If directive has NO parameters → matches any request (broad grant)
        if (directive.Parameters.Count == 0)
        {
            return true;
        }

        // 3. If directive HAS parameters → ALL directive parameters must match request parameters
        if (requestParameters is null || requestParameters.Count == 0)
        {
            return false;
        }

        foreach (var kvp in directive.Parameters)
        {
            if (!requestParameters.TryGetValue(kvp.Key, out var requestValue))
            {
                return false;
            }

            if (!string.Equals(kvp.Value, requestValue, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Determines whether a directive path matches the requested permission path.
    /// </summary>
    private static bool PathMatches(string directivePath, string requestedPath)
    {
        // 1. Exact match
        if (string.Equals(directivePath, requestedPath, StringComparison.Ordinal))
        {
            return true;
        }

        // 2. Root _read: If directive is "_read", matches any path ending in ":_read" OR any RLeaf permission
        if (string.Equals(directivePath, "_read", StringComparison.Ordinal))
        {
            return requestedPath.EndsWith(":_read", StringComparison.Ordinal) ||
                   PermissionCache.ReadLeafPaths.Contains(requestedPath);
        }

        // 3. Root _write: If directive is "_write", matches any path ending in ":_write" OR any WLeaf permission
        if (string.Equals(directivePath, "_write", StringComparison.Ordinal))
        {
            return requestedPath.EndsWith(":_write", StringComparison.Ordinal) ||
                   PermissionCache.WriteLeafPaths.Contains(requestedPath);
        }

        // 4. Hierarchical: requested path starts with directive path + ":"
        //    (e.g., directive "api:user" matches "api:user:profile:read")
        if (requestedPath.StartsWith(directivePath + ":", StringComparison.Ordinal))
        {
            return true;
        }

        // 5. Scoped _read: If directive ends in ":_read", matches child RLeaf permissions under that parent
        if (directivePath.EndsWith(":_read", StringComparison.Ordinal))
        {
            var parentPath = directivePath[..^6]; // Remove ":_read"

            // Check if requested path is under this parent and is an RLeaf
            if (requestedPath.StartsWith(parentPath + ":", StringComparison.Ordinal))
            {
                return PermissionCache.ReadLeafPaths.Contains(requestedPath);
            }

            // Also match any nested :_read scopes
            if (requestedPath.StartsWith(parentPath + ":", StringComparison.Ordinal) &&
                requestedPath.EndsWith(":_read", StringComparison.Ordinal))
            {
                return true;
            }
        }

        // 6. Scoped _write: If directive ends in ":_write", matches child WLeaf permissions under that parent
        if (directivePath.EndsWith(":_write", StringComparison.Ordinal))
        {
            var parentPath = directivePath[..^7]; // Remove ":_write"

            // Check if requested path is under this parent and is a WLeaf
            if (requestedPath.StartsWith(parentPath + ":", StringComparison.Ordinal))
            {
                return PermissionCache.WriteLeafPaths.Contains(requestedPath);
            }

            // Also match any nested :_write scopes
            if (requestedPath.StartsWith(parentPath + ":", StringComparison.Ordinal) &&
                requestedPath.EndsWith(":_write", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}

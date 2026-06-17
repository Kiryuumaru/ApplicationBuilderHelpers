using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Domain.Authorization.Constants;
using Domain.Authorization.Enums;
using Domain.Authorization.Models;

namespace Application.Authorization.Services;

internal sealed partial class PermissionService
{
    public Task<IReadOnlyCollection<Permission>> GetPermissionTreeAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(PermissionCache.TreeRoots);

    public Task<IReadOnlyCollection<string>> GetAllPermissionIdentifiersAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(PermissionCache.AssignableIdentifiers);

    public Task<bool> ValidatePermissionsAsync(IEnumerable<string> permissionIdentifiers, CancellationToken cancellationToken = default)
    {
        if (permissionIdentifiers is null)
        {
            return Task.FromResult(false);
        }

        foreach (var identifier in permissionIdentifiers)
        {
            if (string.IsNullOrWhiteSpace(identifier))
            {
                return Task.FromResult(false);
            }

            if (!Permission.TryParseIdentifier(identifier, out var parsed))
            {
                return Task.FromResult(false);
            }

            if (!PermissionCache.ByPath.TryGetValue(parsed.Canonical, out var permission))
            {
                return Task.FromResult(false);
            }

            if (permission.AccessCategory == PermissionAccessCategory.Unspecified)
            {
                return Task.FromResult(false);
            }

            if (!AreParametersValid(permission, parsed.Parameters, out _))
            {
                return Task.FromResult(false);
            }
        }

        return Task.FromResult(true);
    }

    public Task<IReadOnlyCollection<Permission>> ResolvePermissionsAsync(
        IEnumerable<string> permissionIdentifiers,
        CancellationToken cancellationToken = default)
    {
        var normalizedPermissions = NormalizeAndValidate(permissionIdentifiers, allowEmpty: true);
        if (normalizedPermissions.Length == 0)
        {
            return Task.FromResult<IReadOnlyCollection<Permission>>([]);
        }

        var resolved = new List<Permission>(normalizedPermissions.Length);
        foreach (var identifier in normalizedPermissions)
        {
            if (!Permission.TryParseIdentifier(identifier, out var parsed))
            {
                continue;
            }

            if (PermissionCache.ByPath.TryGetValue(parsed.Canonical, out var permission))
            {
                resolved.Add(permission);
            }
        }

        return Task.FromResult<IReadOnlyCollection<Permission>>(resolved.Count == 0
            ? Array.Empty<Permission>()
            : [.. resolved]);
    }

    private static string[] NormalizeAndValidate(IEnumerable<string> permissionIdentifiers, bool allowEmpty)
    {
        if (permissionIdentifiers is null)
        {
            return [];
        }

        var unique = new HashSet<string>(StringComparer.Ordinal);
        var ordered = new List<string>();

        foreach (var rawIdentifier in permissionIdentifiers)
        {
            if (string.IsNullOrWhiteSpace(rawIdentifier))
            {
                throw new ArgumentException("Permission identifiers cannot contain null or whitespace entries.", nameof(permissionIdentifiers));
            }

            Permission.ParsedIdentifier parsed;
            try
            {
                parsed = Permission.ParseIdentifier(rawIdentifier);
            }
            catch (FormatException ex)
            {
                throw new ArgumentException($"Permission identifier '{rawIdentifier}' has an invalid format: {ex.Message}", nameof(permissionIdentifiers), ex);
            }

            if (!PermissionCache.ByPath.TryGetValue(parsed.Canonical, out var permission))
            {
                throw new ArgumentException($"Unknown permission identifier '{parsed.Identifier}'.", nameof(permissionIdentifiers));
            }

            if (permission.AccessCategory == PermissionAccessCategory.Unspecified)
            {
                throw new ArgumentException($"Permission identifier '{parsed.Identifier}' is not assignable. Select a specific scope or operation.", nameof(permissionIdentifiers));
            }

            if (!AreParametersValid(permission, parsed.Parameters, out var invalidParameter))
            {
                throw new ArgumentException($"Permission identifier '{parsed.Identifier}' specifies unsupported parameter '{invalidParameter}'.", nameof(permissionIdentifiers));
            }

            if (unique.Add(parsed.Identifier))
            {
                ordered.Add(parsed.Identifier);
            }
        }

        if (!allowEmpty && ordered.Count == 0)
        {
            throw new ArgumentException("At least one permission identifier must be provided.", nameof(permissionIdentifiers));
        }

        return ordered.Count == 0
            ? []
            : [.. ordered];
    }
}

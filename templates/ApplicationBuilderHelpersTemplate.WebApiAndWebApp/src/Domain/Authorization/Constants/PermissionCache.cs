using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Domain.Authorization.Enums;
using Domain.Authorization.Models;

namespace Domain.Authorization.Constants;

/// <summary>
/// Shared cached lookups for the permission tree. Built once at startup.
/// </summary>
public static class PermissionCache
{
    /// <summary>
    /// Gets a dictionary mapping permission paths to Permission objects.
    /// </summary>
    public static IReadOnlyDictionary<string, Permission> ByPath { get; }

    /// <summary>
    /// Gets all permission paths that are Read-category leaves (no children).
    /// </summary>
    public static IReadOnlyCollection<string> ReadLeafPaths { get; }

    /// <summary>
    /// Gets all permission paths that are Write-category leaves (no children).
    /// </summary>
    public static IReadOnlyCollection<string> WriteLeafPaths { get; }

    /// <summary>
    /// Gets all assignable permission identifiers (non-Unspecified access categories), sorted.
    /// </summary>
    public static IReadOnlyCollection<string> AssignableIdentifiers { get; }

    /// <summary>
    /// Gets the permission tree roots as a read-only collection.
    /// </summary>
    public static IReadOnlyCollection<Permission> TreeRoots { get; }

    static PermissionCache()
    {
        var allPermissions = Permissions.GetAll();
        var lookup = new Dictionary<string, Permission>(allPermissions.Count, StringComparer.Ordinal);
        var readLeafs = new HashSet<string>(StringComparer.Ordinal);
        var writeLeafs = new HashSet<string>(StringComparer.Ordinal);
        var assignable = new List<string>();

        foreach (var permission in allPermissions)
        {
            lookup[permission.Path] = permission;

            if (!permission.HasChildren)
            {
                if (permission.AccessCategory == PermissionAccessCategory.Read)
                {
                    readLeafs.Add(permission.Path);
                }
                else if (permission.AccessCategory == PermissionAccessCategory.Write)
                {
                    writeLeafs.Add(permission.Path);
                }
            }

            if (permission.AccessCategory != PermissionAccessCategory.Unspecified)
            {
                assignable.Add(permission.Path);
            }
        }

        assignable.Sort(StringComparer.Ordinal);

        ByPath = new ReadOnlyDictionary<string, Permission>(lookup);
        ReadLeafPaths = readLeafs;
        WriteLeafPaths = writeLeafs;
        AssignableIdentifiers = assignable.AsReadOnly();
        TreeRoots = new ReadOnlyCollection<Permission>([.. Permissions.PermissionTreeRoots]);
    }
}

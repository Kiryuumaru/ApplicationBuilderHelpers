using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Domain.Authorization.Constants;
using Domain.Shared.Constants;
using Domain.Shared.Exceptions;
using Application.Authorization.Interfaces.Inbound;
using Application.Authorization.Interfaces.Outbound;
using Domain.Authorization.Entities;
using Domain.Authorization.Enums;
using Domain.Authorization.Interfaces;
using Domain.Authorization.Models;
using Domain.Authorization.Utilities;
using Domain.Authorization.ValueObjects;
using Domain.Identity.Enums;
using TokenClaimTypes = Domain.Identity.Constants.TokenClaimTypes;

namespace Application.Authorization.Services;

internal sealed partial class PermissionService(
    ITokenProvider tokenProvider,
    IRoleRepository roleRepository) : IPermissionService
{
    private static readonly ReadOnlyDictionary<string, HashSet<string>> ReachableParameterLookup;
    private static readonly HashSet<string> EmptyParameterNameSet = new(StringComparer.Ordinal);

    static PermissionService()
    {
        ReachableParameterLookup = BuildReachableParameterLookup(PermissionCache.TreeRoots);
    }

    private static HashSet<string> CollectRelevantParameterNames(Permission permission)
        => new(permission.GetParameterHierarchy(), StringComparer.Ordinal);

    private static bool AreParametersValid(Permission permission, IReadOnlyDictionary<string, string> parameters, out string? invalidParameter)
    {
        if (parameters.Count == 0)
        {
            invalidParameter = null;
            return true;
        }

        if (permission.Identifier is Permissions.ReadScopeIdentifier or Permissions.WriteScopeIdentifier && permission.Parent is null)
        {
            invalidParameter = null;
            return true;
        }

        var allowedAncestors = CollectRelevantParameterNames(permission);
        var reachable = GetReachableParameterNames(permission);

        if (allowedAncestors.Count == 0 && reachable.Count == 0)
        {
            invalidParameter = null;
            return true;
        }

        foreach (var parameter in parameters.Keys)
        {
            if (allowedAncestors.Contains(parameter))
            {
                continue;
            }

            if (reachable.Contains(parameter))
            {
                continue;
            }

            invalidParameter = parameter;
            return false;
        }

        invalidParameter = null;
        return true;
    }

    private static ReadOnlyDictionary<string, HashSet<string>> BuildReachableParameterLookup(IEnumerable<Permission> roots)
    {
        var cache = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        foreach (var root in roots)
        {
            if (root.Identifier is Permissions.ReadScopeIdentifier or Permissions.WriteScopeIdentifier)
            {
                continue;
            }

            ComputeReachableParameters(root, cache);
        }

        foreach (var permission in roots.SelectMany(static root => root.Traverse()))
        {
            if (cache.ContainsKey(permission.Path))
            {
                continue;
            }

            if (permission.Identifier is Permissions.ReadScopeIdentifier or Permissions.WriteScopeIdentifier)
            {
                var parent = permission.Parent;
                if (parent is not null && cache.TryGetValue(parent.Path, out var parentParameters))
                {
                    cache[permission.Path] = parentParameters;
                }
                else
                {
                    cache[permission.Path] = EmptyParameterNameSet;
                }
            }
            else
            {
                cache[permission.Path] = new HashSet<string>(permission.Parameters, StringComparer.Ordinal);
            }
        }

        return new ReadOnlyDictionary<string, HashSet<string>>(cache);
    }

    private static HashSet<string> ComputeReachableParameters(Permission permission, Dictionary<string, HashSet<string>> cache)
    {
        if (cache.TryGetValue(permission.Path, out var existing))
        {
            return existing;
        }

        var names = new HashSet<string>(permission.Parameters, StringComparer.Ordinal);

        foreach (var child in permission.Permissions)
        {
            if (child.Identifier is Permissions.ReadScopeIdentifier or Permissions.WriteScopeIdentifier)
            {
                continue;
            }

            var childNames = ComputeReachableParameters(child, cache);
            names.UnionWith(childNames);
        }

        cache[permission.Path] = names;
        return names;
    }

    private static HashSet<string> GetReachableParameterNames(Permission permission)
    {
        if (ReachableParameterLookup.TryGetValue(permission.Path, out var parameters))
        {
            return parameters;
        }

        return EmptyParameterNameSet;
    }
}

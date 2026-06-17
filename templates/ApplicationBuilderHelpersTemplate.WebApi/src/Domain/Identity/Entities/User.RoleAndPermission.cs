using Domain.Authorization.ValueObjects;
using Domain.Identity.Models;
using Domain.Identity.ValueObjects;

namespace Domain.Identity.Entities;

public sealed partial class User
{
    public void GrantPermission(UserPermissionGrant grant)
    {
        ArgumentNullException.ThrowIfNull(grant);
        if (_permissionGrants.Add(grant))
        {
            MarkAsModified();
        }
    }

    public bool RevokePermission(string permissionIdentifier)
    {
        ArgumentException.ThrowIfNullOrEmpty(permissionIdentifier);
        var canonical = permissionIdentifier.Trim();
        var removed = _permissionGrants.RemoveWhere(grant => string.Equals(grant.Identifier, canonical, StringComparison.Ordinal)) > 0;
        if (removed)
        {
            MarkAsModified();
        }

        return removed;
    }

    public IReadOnlyCollection<string> GetPermissionIdentifiers()
        => [.. _permissionGrants
            .Select(grant => grant.Identifier)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(identifier => identifier, StringComparer.Ordinal)];

    public IReadOnlyCollection<ScopeDirective> BuildEffectiveScopeDirectives(IEnumerable<UserRoleResolution>? roleResolutions)
    {
        var directives = new List<ScopeDirective>();

        // Add direct permission grants as scope directives (respecting Allow/Deny type)
        foreach (var grant in _permissionGrants)
        {
            directives.Add(grant.ToScopeDirective());
        }

        // Add directives from roles
        if (roleResolutions is not null)
        {
            foreach (var resolution in roleResolutions)
            {
                if (resolution?.Role is null)
                {
                    continue;
                }

                foreach (var directive in resolution.Role.ExpandScope(resolution.ParameterValues))
                {
                    directives.Add(directive);
                }
            }
        }

        return directives;
    }

    public IReadOnlyCollection<string> BuildEffectivePermissions(IEnumerable<UserRoleResolution>? roleResolutions)
    {
        var identifiers = new HashSet<string>(StringComparer.Ordinal);
        foreach (var permission in GetPermissionIdentifiers())
        {
            identifiers.Add(permission);
        }

        if (roleResolutions is not null)
        {
            foreach (var resolution in roleResolutions)
            {
                if (resolution?.Role is null)
                {
                    continue;
                }

                foreach (var directive in resolution.Role.ExpandScope(resolution.ParameterValues))
                {
                    var identifier = directive.ToPermissionIdentifier();
                    if (identifier is not null)
                    {
                        identifiers.Add(identifier);
                    }
                }
            }
        }

        return [.. identifiers.OrderBy(static id => id, StringComparer.Ordinal)];
    }

    public bool AssignRole(Guid roleId, IReadOnlyDictionary<string, string?>? parameterValues = null)
    {
        var assignment = UserRoleAssignment.Create(roleId, parameterValues);
        if (_roleAssignments.Add(assignment))
        {
            MarkAsModified();
            return true;
        }

        return false;
    }

    public bool RemoveRole(Guid roleId)
    {
        var removed = _roleAssignments.RemoveWhere(assignment => assignment.RoleId == roleId) > 0;
        if (removed)
        {
            MarkAsModified();
            return true;
        }

        return false;
    }

    public void ClearRoles()
    {
        if (_roleAssignments.Count > 0)
        {
            _roleAssignments.Clear();
            MarkAsModified();
        }
    }
}

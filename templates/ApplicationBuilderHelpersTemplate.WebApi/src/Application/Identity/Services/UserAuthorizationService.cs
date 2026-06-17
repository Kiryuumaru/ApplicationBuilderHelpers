using Application.Authorization.Interfaces.Inbound;
using Application.Authorization.Utilities;
using Application.Identity.Interfaces.Inbound;
using Application.Identity.Models;
using Domain.Authorization.Interfaces;
using Domain.Identity.Interfaces;
using Domain.Identity.ValueObjects;
using Domain.Shared.Exceptions;

namespace Application.Identity.Services;

internal sealed class UserAuthorizationService(
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IIdentityUnitOfWork unitOfWork,
    IUserRoleResolver userRoleResolver) : IUserAuthorizationService
{
    public async Task AssignRoleAsync(Guid userId, RoleAssignmentRequest assignment, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(assignment);

        var user = await userRepository.FindByIdAsync(userId, cancellationToken).ConfigureAwait(false)
            ?? throw new EntityNotFoundException("User", userId.ToString());

        var role = await roleRepository.GetByCodeAsync(assignment.RoleCode, cancellationToken).ConfigureAwait(false)
            ?? throw new EntityNotFoundException("Role", assignment.RoleCode);

        // Validate required parameters
        RoleValidationHelper.ValidateRoleParameters(role, assignment.ParameterValues);

        user.AssignRole(role.Id, assignment.ParameterValues);
        userRepository.Update(user);
        await unitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task RemoveRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByIdAsync(userId, cancellationToken).ConfigureAwait(false)
            ?? throw new EntityNotFoundException("User", userId.ToString());

        user.RemoveRole(roleId);
        userRepository.Update(user);
        await unitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyCollection<string>> GetEffectivePermissionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByIdAsync(userId, cancellationToken).ConfigureAwait(false)
            ?? throw new EntityNotFoundException("User", userId.ToString());

        var roleResolutions = await userRoleResolver.ResolveRolesAsync(user, cancellationToken).ConfigureAwait(false);

        var permissions = new HashSet<string>(StringComparer.Ordinal);

        // Add direct permission grants from the user
        foreach (var grant in user.PermissionGrants)
        {
            // Direct grants are added as "allow" directives
            permissions.Add($"allow;{grant.Identifier}");
        }

        // Add permissions from roles
        foreach (var resolution in roleResolutions)
        {
            var scope = resolution.Role.ExpandScope(resolution.ParameterValues);
            foreach (var directive in scope)
            {
                permissions.Add(directive.ToString());
            }
        }

        return [.. permissions.OrderBy(p => p, StringComparer.Ordinal)];
    }

    public async Task GrantPermissionAsync(Guid userId, string permissionIdentifier, bool isAllow, string? description, string? grantedBy, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(permissionIdentifier);

        var user = await userRepository.FindByIdAsync(userId, cancellationToken).ConfigureAwait(false)
            ?? throw new EntityNotFoundException("User", userId.ToString());

        var grant = isAllow
            ? UserPermissionGrant.Allow(permissionIdentifier, description, grantedBy)
            : UserPermissionGrant.Deny(permissionIdentifier, description, grantedBy);
        user.GrantPermission(grant);
        userRepository.Update(user);
        await unitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> RevokePermissionAsync(Guid userId, string permissionIdentifier, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(permissionIdentifier);

        var user = await userRepository.FindByIdAsync(userId, cancellationToken).ConfigureAwait(false)
            ?? throw new EntityNotFoundException("User", userId.ToString());

        var revoked = user.RevokePermission(permissionIdentifier);
        if (revoked)
        {
            userRepository.Update(user);
            await unitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        return revoked;
    }

    public async Task<IReadOnlyCollection<string>> GetFormattedRoleClaimsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByIdAsync(userId, cancellationToken).ConfigureAwait(false)
            ?? throw new EntityNotFoundException("User", userId.ToString());

        var roleResolutions = await userRoleResolver.ResolveRolesAsync(user, cancellationToken).ConfigureAwait(false);
        return roleResolutions.Select(r => r.ToFormattedClaim()).OrderBy(r => r, StringComparer.Ordinal).ToArray();
    }

    public async Task<IReadOnlyCollection<string>> GetDirectPermissionScopesAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByIdAsync(userId, cancellationToken).ConfigureAwait(false)
            ?? throw new EntityNotFoundException("User", userId.ToString());

        // Return direct permission grants as scope directives (preserving Allow/Deny type)
        // Role-derived scopes are NOT included - they are resolved at runtime
        return user.PermissionGrants
            .Select(g => g.ToScopeDirective().ToString())
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToArray();
    }

    public async Task<UserAuthorizationData> GetAuthorizationDataAsync(Guid userId, CancellationToken cancellationToken)
    {
        // Single database call to get the user
        var user = await userRepository.FindByIdAsync(userId, cancellationToken).ConfigureAwait(false)
            ?? throw new EntityNotFoundException("User", userId.ToString());

        // Single call to resolve roles
        var roleResolutions = await userRoleResolver.ResolveRolesAsync(user, cancellationToken).ConfigureAwait(false);

        // Build formatted role claims (e.g., "USER;roleUserId=abc123")
        var formattedRoles = roleResolutions
            .Select(r => r.ToFormattedClaim())
            .OrderBy(r => r, StringComparer.Ordinal)
            .ToArray();

        // Build direct permission scopes (preserving Allow/Deny type)
        var directScopes = user.PermissionGrants
            .Select(g => g.ToScopeDirective().ToString())
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToArray();

        // Build effective permissions (for display)
        var effectivePermissions = new HashSet<string>(StringComparer.Ordinal);
        foreach (var grant in user.PermissionGrants)
        {
            effectivePermissions.Add(grant.ToScopeDirective().ToString());
        }
        foreach (var resolution in roleResolutions)
        {
            var scope = resolution.Role.ExpandScope(resolution.ParameterValues);
            foreach (var directive in scope)
            {
                effectivePermissions.Add(directive.ToString());
            }
        }

        return new UserAuthorizationData
        {
            UserId = user.Id,
            Username = user.UserName,
            Email = user.Email,
            IsAnonymous = user.IsAnonymous,
            FormattedRoles = formattedRoles,
            DirectPermissionScopes = directScopes,
            EffectivePermissions = [.. effectivePermissions.OrderBy(p => p, StringComparer.Ordinal)]
        };
    }
}

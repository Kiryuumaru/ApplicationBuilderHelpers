using Application.Identity.Models;

namespace Application.Identity.Interfaces.Inbound;

/// <summary>
/// Service for user authorization management operations (roles and permissions).
/// Split from IIdentityService to follow single responsibility principle.
/// </summary>
public interface IUserAuthorizationService
{
    /// <summary>
    /// Assigns a role to a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="assignment">The role assignment request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AssignRoleAsync(Guid userId, RoleAssignmentRequest assignment, CancellationToken cancellationToken);

    /// <summary>
    /// Removes a role from a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="roleId">The role ID to remove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RemoveRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets all effective permissions for a user (from all assigned roles).
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of permission identifiers.</returns>
    Task<IReadOnlyCollection<string>> GetEffectivePermissionsAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Grants a direct permission to a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="permissionIdentifier">The permission identifier to grant.</param>
    /// <param name="isAllow">True for Allow grant, false for Deny grant.</param>
    /// <param name="description">Optional description for the grant.</param>
    /// <param name="grantedBy">Optional identifier of who granted the permission.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task GrantPermissionAsync(Guid userId, string permissionIdentifier, bool isAllow, string? description, string? grantedBy, CancellationToken cancellationToken);

    /// <summary>
    /// Revokes a direct permission from a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="permissionIdentifier">The permission identifier to revoke.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the permission was revoked, false if it was not found.</returns>
    Task<bool> RevokePermissionAsync(Guid userId, string permissionIdentifier, CancellationToken cancellationToken);

    /// <summary>
    /// Gets formatted role claims for a user in inline format (e.g., "USER;roleUserId=abc123").
    /// These are used directly as JWT role claim values.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of formatted role claim strings.</returns>
    Task<IReadOnlyCollection<string>> GetFormattedRoleClaimsAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets only the direct (manually-granted) permission scopes for a user.
    /// Does NOT include role-derived scopes. Used for JWT scope claims.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of direct permission scope directives.</returns>
    Task<IReadOnlyCollection<string>> GetDirectPermissionScopesAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets all authorization data for a user in a single database call.
    /// Includes formatted roles, direct scopes, and effective permissions.
    /// Use this instead of calling GetFormattedRoleClaimsAsync, GetDirectPermissionScopesAsync,
    /// and GetEffectivePermissionsAsync separately.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Combined authorization data for the user.</returns>
    Task<UserAuthorizationData> GetAuthorizationDataAsync(Guid userId, CancellationToken cancellationToken);
}

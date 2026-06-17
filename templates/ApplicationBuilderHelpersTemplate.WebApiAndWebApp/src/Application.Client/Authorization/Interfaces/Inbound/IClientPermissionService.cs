namespace Application.Client.Authorization.Interfaces.Inbound;

/// <summary>
/// Client-side service for evaluating user permissions from the current auth state.
/// </summary>
public interface IClientPermissionService
{
    /// <summary>
    /// Determines whether the current user has the specified permission.
    /// </summary>
    /// <param name="permissionPath">The permission path to check (e.g., "api:iam:users:list").</param>
    /// <returns>True if access is granted; otherwise false.</returns>
    bool HasPermission(string permissionPath);

    /// <summary>
    /// Determines whether the current user has any of the specified permissions.
    /// </summary>
    /// <param name="permissionPaths">The permission paths to check.</param>
    /// <returns>True if at least one permission is granted; otherwise false.</returns>
    bool HasAnyPermission(params string[] permissionPaths);

    /// <summary>
    /// Determines whether the current user has all of the specified permissions.
    /// </summary>
    /// <param name="permissionPaths">The permission paths to check.</param>
    /// <returns>True if all permissions are granted; otherwise false.</returns>
    bool HasAllPermissions(params string[] permissionPaths);
}

using Application.Client.Authorization.Models;

namespace Application.Client.Authorization.Interfaces.Inbound;

/// <summary>
/// Interface for IAM role operations.
/// </summary>
public interface IRolesClient
{
    /// <summary>
    /// Lists all roles.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of roles.</returns>
    Task<List<IamRole>> ListRolesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a role by ID.
    /// </summary>
    /// <param name="roleId">The role ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The role, or null if not found.</returns>
    Task<IamRole?> GetRoleAsync(Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new role.
    /// </summary>
    /// <param name="request">The role creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created role.</returns>
    Task<IamRole?> CreateRoleAsync(CreateRoleRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a role.
    /// </summary>
    /// <param name="roleId">The role ID.</param>
    /// <param name="request">The update request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated role.</returns>
    Task<IamRole?> UpdateRoleAsync(Guid roleId, UpdateRoleRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a role.
    /// </summary>
    /// <param name="roleId">The role ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if successful.</returns>
    Task<bool> DeleteRoleAsync(Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Assigns a role to a user.
    /// </summary>
    /// <param name="request">The role assignment request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if successful.</returns>
    Task<bool> AssignRoleAsync(AssignRoleRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unassigns a role from a user.
    /// </summary>
    /// <param name="request">The role unassignment request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if successful.</returns>
    Task<bool> UnassignRoleAsync(UnassignRoleRequest request, CancellationToken cancellationToken = default);
}

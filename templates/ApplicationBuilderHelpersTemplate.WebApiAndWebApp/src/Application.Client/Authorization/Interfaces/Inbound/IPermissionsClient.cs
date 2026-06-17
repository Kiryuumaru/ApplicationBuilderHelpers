using Application.Client.Authorization.Models;

namespace Application.Client.Authorization.Interfaces.Inbound;

/// <summary>
/// Interface for IAM permission operations.
/// </summary>
public interface IPermissionsClient
{
    /// <summary>
    /// Lists all available permissions in the system.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Hierarchical list of permissions.</returns>
    Task<List<IamPermission>> ListPermissionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Grants a direct permission to a user.
    /// </summary>
    /// <param name="request">The grant request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if successful.</returns>
    Task<bool> GrantPermissionAsync(GrantPermissionRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes a direct permission from a user.
    /// </summary>
    /// <param name="request">The revoke request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if successful.</returns>
    Task<bool> RevokePermissionAsync(RevokePermissionRequest request, CancellationToken cancellationToken = default);
}

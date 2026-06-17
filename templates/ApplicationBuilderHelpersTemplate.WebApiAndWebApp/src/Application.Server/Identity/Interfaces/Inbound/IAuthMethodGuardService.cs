namespace Application.Server.Identity.Interfaces.Inbound;

/// <summary>
/// Guard service for validating authentication method operations.
/// Ensures users cannot remove their last authentication method.
/// </summary>
public interface IAuthMethodGuardService
{
    /// <summary>
    /// Checks if a passkey can be removed from the user's account.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="credentialId">The passkey credential ID to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the passkey can be removed; false if it's the last auth method.</returns>
    Task<bool> CanRemovePasskeyAsync(Guid userId, Guid credentialId, CancellationToken cancellationToken);

    /// <summary>
    /// Checks if an external OAuth provider can be unlinked from the user's account.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="provider">The OAuth provider to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the provider can be unlinked; false if it's the last auth method.</returns>
    Task<bool> CanUnlinkProviderAsync(Guid userId, string provider, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the count of authentication methods for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Total count of authentication methods.</returns>
    Task<int> GetAuthMethodCountAsync(Guid userId, CancellationToken cancellationToken);
}

using Application.Client.Identity.Models;

namespace Application.Client.Identity.Interfaces.Inbound;

/// <summary>
/// Interface for two-factor authentication operations.
/// </summary>
public interface ITwoFactorClient
{
    /// <summary>
    /// Gets the 2FA setup information (shared key and QR code URI).
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>2FA setup information.</returns>
    Task<TwoFactorSetupInfo?> GetSetupInfoAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enables 2FA by verifying a code from the authenticator app.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="verificationCode">The TOTP code from the authenticator app.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing recovery codes on success.</returns>
    Task<EnableTwoFactorResult> EnableAsync(Guid userId, string verificationCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Disables 2FA for the user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="password">The user's password for verification.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if successful.</returns>
    Task<bool> DisableAsync(Guid userId, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates new recovery codes (invalidates previous codes).
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of new recovery codes.</returns>
    Task<List<string>> GenerateRecoveryCodesAsync(Guid userId, CancellationToken cancellationToken = default);
}

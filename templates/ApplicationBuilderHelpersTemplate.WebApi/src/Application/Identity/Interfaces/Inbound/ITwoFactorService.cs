using Application.Identity.Models;

namespace Application.Identity.Interfaces.Inbound;

/// <summary>
/// Service for two-factor authentication operations.
/// Split from IIdentityService to follow single responsibility principle.
/// </summary>
public interface ITwoFactorService
{
    /// <summary>
    /// Sets up 2FA for a user, generating the shared secret.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Setup information including shared key and QR code data.</returns>
    Task<TwoFactorSetupInfo> Setup2faAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Enables 2FA for a user after verifying the code.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="verificationCode">The TOTP code to verify.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Recovery codes for backup access.</returns>
    Task<IReadOnlyCollection<string>> Enable2faAsync(Guid userId, string verificationCode, CancellationToken cancellationToken);

    /// <summary>
    /// Disables 2FA for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task Disable2faAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Verifies a 2FA code for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="code">The TOTP code or recovery code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the code is valid.</returns>
    Task<bool> Verify2faCodeAsync(Guid userId, string code, CancellationToken cancellationToken);

    /// <summary>
    /// Generates new recovery codes for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>New recovery codes.</returns>
    Task<IReadOnlyCollection<string>> GenerateRecoveryCodesAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the count of remaining recovery codes for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of remaining recovery codes.</returns>
    Task<int> GetRecoveryCodeCountAsync(Guid userId, CancellationToken cancellationToken);
}

using Application.Client.Identity.Models;

namespace Application.Client.Identity.Interfaces.Inbound;

/// <summary>
/// Interface for authentication API operations.
/// </summary>
public interface IAuthenticationClient
{
    /// <summary>
    /// Attempts to login with username/email and password.
    /// </summary>
    Task<LoginResult> LoginAsync(string usernameOrEmail, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a new user account.
    /// </summary>
    Task<LoginResult> RegisterAsync(string email, string username, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes the access token using a refresh token.
    /// </summary>
    Task<LoginResult> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs out the current user (invalidates tokens on server).
    /// </summary>
    Task LogoutAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Initiates password reset for the given email.
    /// </summary>
    Task<bool> ForgotPasswordAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets password using a reset token.
    /// </summary>
    Task<bool> ResetPasswordAsync(string email, string token, string newPassword, CancellationToken cancellationToken = default);

    /// <summary>
    /// Confirms two-factor authentication code.
    /// </summary>
    Task<LoginResult> ConfirmTwoFactorAsync(string code, string twoFactorToken, CancellationToken cancellationToken = default);
}

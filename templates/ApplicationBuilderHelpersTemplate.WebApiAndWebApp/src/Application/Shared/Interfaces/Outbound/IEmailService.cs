namespace Application.Shared.Interfaces.Outbound;

/// <summary>
/// Service for sending emails.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends a password reset link email.
    /// </summary>
    /// <param name="email">The recipient email address.</param>
    /// <param name="resetLink">The password reset link.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendPasswordResetLinkAsync(string email, string resetLink, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a password reset code email.
    /// </summary>
    /// <param name="email">The recipient email address.</param>
    /// <param name="resetCode">The password reset code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendPasswordResetCodeAsync(string email, string resetCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an email confirmation link.
    /// </summary>
    /// <param name="email">The recipient email address.</param>
    /// <param name="confirmationLink">The email confirmation link.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendEmailConfirmationLinkAsync(string email, string confirmationLink, CancellationToken cancellationToken = default);
}

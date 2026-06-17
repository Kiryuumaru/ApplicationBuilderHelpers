namespace Application.Identity.Interfaces.Inbound;

/// <summary>
/// Service for building frontend URLs for authentication flows.
/// </summary>
public interface IFrontendUrlBuilder
{
    /// <summary>
    /// Builds a password reset URL with the given email and token.
    /// </summary>
    /// <param name="email">The user's email address.</param>
    /// <param name="token">The password reset token (will be URL-encoded).</param>
    /// <returns>The complete password reset URL.</returns>
    string BuildPasswordResetUrl(string email, string token);

    /// <summary>
    /// Builds an email verification URL with the given email and token.
    /// </summary>
    /// <param name="email">The user's email address.</param>
    /// <param name="token">The email verification token (will be URL-encoded).</param>
    /// <returns>The complete email verification URL.</returns>
    string BuildEmailVerificationUrl(string email, string token);
}

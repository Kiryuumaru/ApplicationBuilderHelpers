namespace Application.Server.Identity.Models;

/// <summary>
/// Options for configuring frontend URLs.
/// </summary>
public sealed class FrontendUrlOptions
{
    /// <summary>
    /// The base URL for the frontend application (e.g., "https://example.com").
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// The path for password reset page (default: "/reset-password").
    /// </summary>
    public string PasswordResetPath { get; set; } = "/reset-password";

    /// <summary>
    /// The path for email verification page (default: "/verify-email").
    /// </summary>
    public string EmailVerificationPath { get; set; } = "/verify-email";
}

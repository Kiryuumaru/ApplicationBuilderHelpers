namespace Application.Client.Identity.Models;

/// <summary>
/// Represents 2FA setup information including the shared key and QR code URI.
/// </summary>
public class TwoFactorSetupInfo
{
    /// <summary>
    /// Gets or sets the shared key for manual entry.
    /// </summary>
    public string SharedKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the formatted shared key (e.g., XXXX-XXXX-XXXX-XXXX).
    /// </summary>
    public string FormattedSharedKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the authenticator URI for QR code generation.
    /// </summary>
    public string AuthenticatorUri { get; set; } = string.Empty;
}

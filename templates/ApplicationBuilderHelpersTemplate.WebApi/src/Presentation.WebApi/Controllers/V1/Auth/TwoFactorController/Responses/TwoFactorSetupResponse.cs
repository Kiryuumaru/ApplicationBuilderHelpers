namespace Presentation.WebApi.Controllers.V1.Auth.TwoFactorController.Responses;

/// <summary>
/// Response model for 2FA setup containing the shared key and authenticator URI.
/// </summary>
public sealed record TwoFactorSetupResponse
{
    /// <summary>
    /// The shared key to be entered manually into an authenticator app.
    /// </summary>
    public required string SharedKey { get; init; }

    /// <summary>
    /// The formatted shared key with spaces for easier reading.
    /// </summary>
    public required string FormattedSharedKey { get; init; }

    /// <summary>
    /// The otpauth:// URI for generating a QR code to scan with an authenticator app.
    /// </summary>
    public required string AuthenticatorUri { get; init; }
}

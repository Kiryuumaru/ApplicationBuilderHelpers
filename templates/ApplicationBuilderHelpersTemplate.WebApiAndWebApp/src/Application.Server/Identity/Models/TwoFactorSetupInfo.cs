namespace Application.Server.Identity.Models;

/// <summary>
/// Response containing the TOTP setup information for 2FA.
/// </summary>
public sealed record TwoFactorSetupInfo(
    string SharedKey,
    string AuthenticatorUri,
    string FormattedSharedKey);

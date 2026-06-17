namespace Application.Client.Identity.Models;

/// <summary>
/// Response from the passkey registration endpoint.
/// </summary>
public sealed class PasskeyRegistrationResult
{
    /// <summary>
    /// The credential ID of the registered passkey.
    /// </summary>
    public string CredentialId { get; set; } = string.Empty;

    /// <summary>
    /// The name of the registered passkey.
    /// </summary>
    public string CredentialName { get; set; } = string.Empty;
}

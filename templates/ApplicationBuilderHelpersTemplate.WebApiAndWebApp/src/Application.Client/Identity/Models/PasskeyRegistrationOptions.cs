namespace Application.Client.Identity.Models;

/// <summary>
/// Response from the passkey registration options endpoint.
/// </summary>
public sealed class PasskeyRegistrationOptions
{
    /// <summary>
    /// The challenge ID to use when completing registration.
    /// </summary>
    public Guid ChallengeId { get; set; }

    /// <summary>
    /// The WebAuthn options JSON to pass to navigator.credentials.create().
    /// </summary>
    public string OptionsJson { get; set; } = string.Empty;
}

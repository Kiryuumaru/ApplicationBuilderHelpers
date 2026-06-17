namespace Presentation.WebApp.Server.Controllers.V1.Auth.PasskeysController.Responses;

/// <summary>
/// Response containing passkey login options.
/// </summary>
/// <param name="ChallengeId">The challenge ID to use when completing login.</param>
/// <param name="OptionsJson">The WebAuthn options JSON to pass to navigator.credentials.get().</param>
public record PasskeyLoginOptionsResponse(
    Guid ChallengeId,
    string OptionsJson
);

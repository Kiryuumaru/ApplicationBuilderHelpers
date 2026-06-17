namespace Presentation.WebApp.Server.Controllers.V1.Auth.PasskeysController.Responses;

/// <summary>
/// Response containing passkey registration options.
/// </summary>
/// <param name="ChallengeId">The challenge ID to use when completing registration.</param>
/// <param name="OptionsJson">The WebAuthn options JSON to pass to navigator.credentials.create().</param>
public record PasskeyRegistrationOptionsResponse(
    Guid ChallengeId,
    string OptionsJson
);

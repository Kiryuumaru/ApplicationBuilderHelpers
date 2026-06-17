namespace Presentation.WebApp.Server.Controllers.V1.Auth.PasskeysController.Responses;

/// <summary>
/// Response after successfully registering a passkey.
/// </summary>
/// <param name="CredentialId">The unique identifier for this passkey.</param>
/// <param name="Name">The name assigned to this passkey.</param>
public record PasskeyRegistrationResponse(
    Guid CredentialId,
    string Name
);

namespace Presentation.WebApp.Server.Controllers.V1.Auth.PasskeysController.Requests;

/// <summary>
/// Request to get passkey authentication options.
/// </summary>
/// <param name="Username">Optional username to filter allowed credentials. If not provided, discoverable credentials will be used.</param>
public record PasskeyLoginOptionsRequest(
    string? Username
);

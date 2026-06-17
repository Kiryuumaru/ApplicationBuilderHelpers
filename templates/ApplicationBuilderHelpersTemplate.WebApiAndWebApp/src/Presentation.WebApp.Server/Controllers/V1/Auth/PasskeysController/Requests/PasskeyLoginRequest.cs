using System.ComponentModel.DataAnnotations;

namespace Presentation.WebApp.Server.Controllers.V1.Auth.PasskeysController.Requests;

/// <summary>
/// Request to complete passkey login with the assertion response.
/// </summary>
/// <param name="ChallengeId">The challenge ID returned from the login options endpoint.</param>
/// <param name="AssertionResponseJson">The JSON-serialized assertion response from the authenticator.</param>
public record PasskeyLoginRequest(
    [Required]
    Guid ChallengeId,
    [Required]
    string AssertionResponseJson
);

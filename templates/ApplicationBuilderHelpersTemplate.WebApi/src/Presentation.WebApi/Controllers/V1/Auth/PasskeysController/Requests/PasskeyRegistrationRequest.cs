using System.ComponentModel.DataAnnotations;

namespace Presentation.WebApi.Controllers.V1.Auth.PasskeysController.Requests;

/// <summary>
/// Request to complete passkey registration with the attestation response.
/// </summary>
/// <param name="ChallengeId">The challenge ID returned from the registration options endpoint.</param>
/// <param name="AttestationResponseJson">The JSON-serialized attestation response from the authenticator.</param>
public sealed record PasskeyRegistrationRequest(
    [Required]
    Guid ChallengeId,
    [Required]
    string AttestationResponseJson
);

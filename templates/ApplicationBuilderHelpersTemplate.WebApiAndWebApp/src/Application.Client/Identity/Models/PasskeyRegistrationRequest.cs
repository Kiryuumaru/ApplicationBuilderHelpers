namespace Application.Client.Identity.Models;

/// <summary>Passkey registration request DTO.</summary>
public sealed class PasskeyRegistrationRequest
{
    public Guid ChallengeId { get; set; }
    public string AttestationResponseJson { get; set; } = string.Empty;
}

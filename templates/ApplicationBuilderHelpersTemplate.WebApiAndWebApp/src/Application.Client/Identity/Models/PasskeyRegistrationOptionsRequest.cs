namespace Application.Client.Identity.Models;

/// <summary>Passkey registration options request DTO.</summary>
public sealed class PasskeyRegistrationOptionsRequest
{
    public string CredentialName { get; set; } = string.Empty;
}

namespace Application.Client.Identity.Models;

/// <summary>Disable 2FA request DTO.</summary>
public sealed class DisableTwoFactorRequest
{
    public string Password { get; set; } = string.Empty;
}

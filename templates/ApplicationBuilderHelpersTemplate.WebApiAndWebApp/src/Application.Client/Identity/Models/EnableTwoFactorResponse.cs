namespace Application.Client.Identity.Models;

/// <summary>Enable 2FA response DTO.</summary>
public sealed class EnableTwoFactorResponse
{
    public List<string> RecoveryCodes { get; set; } = [];
}

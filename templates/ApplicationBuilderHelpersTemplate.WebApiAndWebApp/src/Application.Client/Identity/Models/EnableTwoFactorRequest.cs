namespace Application.Client.Identity.Models;

/// <summary>Enable 2FA request DTO.</summary>
public sealed class EnableTwoFactorRequest
{
    public string VerificationCode { get; set; } = string.Empty;
}

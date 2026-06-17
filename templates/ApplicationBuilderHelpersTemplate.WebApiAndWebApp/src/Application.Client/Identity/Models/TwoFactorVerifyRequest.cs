namespace Application.Client.Identity.Models;

/// <summary>Two-factor verification request DTO.</summary>
public sealed class TwoFactorVerifyRequest
{
    public string Code { get; set; } = string.Empty;
    public string TwoFactorToken { get; set; } = string.Empty;
}

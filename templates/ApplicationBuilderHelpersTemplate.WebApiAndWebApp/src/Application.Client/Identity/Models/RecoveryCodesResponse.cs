namespace Application.Client.Identity.Models;

/// <summary>Recovery codes response DTO.</summary>
public sealed class RecoveryCodesResponse
{
    public List<string> RecoveryCodes { get; set; } = [];
}

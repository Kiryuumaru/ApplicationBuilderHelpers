namespace Application.Client.Identity.Models;

/// <summary>Passkey rename request DTO.</summary>
public sealed class PasskeyRenameRequest
{
    public string Name { get; set; } = string.Empty;
}

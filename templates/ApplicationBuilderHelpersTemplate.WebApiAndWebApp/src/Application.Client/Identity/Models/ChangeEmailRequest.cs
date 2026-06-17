namespace Application.Client.Identity.Models;

/// <summary>Change email request DTO.</summary>
public sealed class ChangeEmailRequest
{
    public string Email { get; set; } = string.Empty;
}

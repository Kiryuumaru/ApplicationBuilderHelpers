namespace Application.Client.Identity.Models;

/// <summary>Change username request DTO.</summary>
public sealed class ChangeUsernameRequest
{
    public string Username { get; set; } = string.Empty;
}

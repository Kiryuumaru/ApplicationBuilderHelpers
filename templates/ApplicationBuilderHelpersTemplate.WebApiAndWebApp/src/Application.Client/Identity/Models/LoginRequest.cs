namespace Application.Client.Identity.Models;

/// <summary>Login request DTO.</summary>
public sealed class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

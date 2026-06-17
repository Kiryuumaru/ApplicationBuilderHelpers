namespace Application.Client.Identity.Models;

/// <summary>Refresh token request DTO.</summary>
public sealed class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

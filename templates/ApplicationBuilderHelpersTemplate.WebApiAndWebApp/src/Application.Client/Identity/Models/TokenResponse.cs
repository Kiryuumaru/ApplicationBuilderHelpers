namespace Application.Client.Identity.Models;

/// <summary>Token response from auth endpoints.</summary>
public sealed class TokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public TokenUserInfo? User { get; set; }
}

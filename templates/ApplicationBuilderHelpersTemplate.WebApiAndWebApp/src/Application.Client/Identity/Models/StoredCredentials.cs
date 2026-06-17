namespace Application.Client.Identity.Models;

/// <summary>
/// Credentials stored in local storage.
/// </summary>
public sealed class StoredCredentials
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTimeOffset AccessTokenExpiry { get; set; }
    public DateTimeOffset RefreshTokenExpiry { get; set; }
    public List<string> Roles { get; set; } = [];
    public List<string> Permissions { get; set; } = [];

    public bool IsValid => !string.IsNullOrEmpty(AccessToken) && AccessTokenExpiry > DateTimeOffset.UtcNow;
    public bool IsRefreshValid => !string.IsNullOrEmpty(RefreshToken) && RefreshTokenExpiry > DateTimeOffset.UtcNow;
}

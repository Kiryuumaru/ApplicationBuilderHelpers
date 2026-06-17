namespace Application.Client.Identity.Models;

/// <summary>
/// Result of a login operation.
/// </summary>
public sealed class LoginResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public string? AccessToken { get; init; }
    public string? RefreshToken { get; init; }
    public int ExpiresIn { get; init; }
    public bool RequiresTwoFactor { get; init; }
    public string? TwoFactorToken { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = [];
    public IReadOnlyList<string> Permissions { get; init; } = [];

    public static LoginResult Succeeded(string accessToken, string refreshToken, int expiresIn, IReadOnlyList<string>? roles = null, IReadOnlyList<string>? permissions = null) => new()
    {
        Success = true,
        AccessToken = accessToken,
        RefreshToken = refreshToken,
        ExpiresIn = expiresIn,
        Roles = roles ?? [],
        Permissions = permissions ?? []
    };

    public static LoginResult Failed(string errorMessage) => new()
    {
        Success = false,
        ErrorMessage = errorMessage
    };

    public static LoginResult TwoFactorRequired(string twoFactorToken) => new()
    {
        Success = false,
        RequiresTwoFactor = true,
        TwoFactorToken = twoFactorToken
    };
}

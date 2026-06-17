namespace Application.Identity.Models;

/// <summary>
/// Result of OAuth callback processing.
/// </summary>
public sealed record OAuthCallbackResult
{
    /// <summary>
    /// Whether the OAuth callback was successful.
    /// </summary>
    public bool Succeeded { get; init; }

    /// <summary>
    /// User information from the OAuth provider (if successful).
    /// </summary>
    public OAuthUserInfo? UserInfo { get; init; }

    /// <summary>
    /// Error message if the callback failed.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Detailed error description if available.
    /// </summary>
    public string? ErrorDescription { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static OAuthCallbackResult Success(OAuthUserInfo userInfo) => new()
    {
        Succeeded = true,
        UserInfo = userInfo
    };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static OAuthCallbackResult Failure(string error, string? description = null) => new()
    {
        Succeeded = false,
        Error = error,
        ErrorDescription = description
    };
}

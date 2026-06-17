namespace Application.Identity.Models;

/// <summary>
/// Result of a token refresh operation.
/// </summary>
public sealed record TokenRefreshResult
{
    /// <summary>
    /// Whether the refresh operation succeeded.
    /// </summary>
    public required bool Succeeded { get; init; }

    /// <summary>
    /// The user ID from the refresh token (if valid).
    /// </summary>
    public Guid? UserId { get; init; }

    /// <summary>
    /// The refreshed tokens (if successful).
    /// </summary>
    public UserTokenResult? Tokens { get; init; }

    /// <summary>
    /// Error code (if failed).
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Error description (if failed).
    /// </summary>
    public string? ErrorDescription { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static TokenRefreshResult Success(Guid userId, UserTokenResult tokens) => new()
    {
        Succeeded = true,
        UserId = userId,
        Tokens = tokens
    };

    /// <summary>
    /// Creates a failure result.
    /// </summary>
    public static TokenRefreshResult Failure(string error, string description) => new()
    {
        Succeeded = false,
        Error = error,
        ErrorDescription = description
    };
}

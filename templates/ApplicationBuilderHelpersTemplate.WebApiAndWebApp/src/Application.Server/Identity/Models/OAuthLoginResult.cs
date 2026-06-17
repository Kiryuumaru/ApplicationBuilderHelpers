namespace Application.Server.Identity.Models;

/// <summary>
/// Result of OAuth login containing user info and whether this is a new registration.
/// </summary>
public sealed record OAuthLoginResult
{
    /// <summary>
    /// Whether the OAuth login was successful.
    /// </summary>
    public bool Succeeded { get; init; }

    /// <summary>
    /// The user ID (for existing or newly created user).
    /// </summary>
    public Guid? UserId { get; init; }

    /// <summary>
    /// The username (for existing or newly created user).
    /// </summary>
    public string? Username { get; init; }

    /// <summary>
    /// Whether this login resulted in a new user registration.
    /// </summary>
    public bool IsNewUser { get; init; }

    /// <summary>
    /// Error message if the login failed.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Detailed error description if available.
    /// </summary>
    public string? ErrorDescription { get; init; }

    /// <summary>
    /// Creates a successful result for an existing user.
    /// </summary>
    public static OAuthLoginResult ExistingUser(Guid userId, string? username) => new()
    {
        Succeeded = true,
        UserId = userId,
        Username = username,
        IsNewUser = false
    };

    /// <summary>
    /// Creates a successful result for a newly registered user.
    /// </summary>
    public static OAuthLoginResult NewUser(Guid userId, string? username) => new()
    {
        Succeeded = true,
        UserId = userId,
        Username = username,
        IsNewUser = true
    };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static OAuthLoginResult Failure(string error, string? description = null) => new()
    {
        Succeeded = false,
        Error = error,
        ErrorDescription = description
    };
}

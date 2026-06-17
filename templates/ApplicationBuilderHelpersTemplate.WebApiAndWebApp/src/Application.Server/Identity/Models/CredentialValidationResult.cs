namespace Application.Server.Identity.Models;

/// <summary>
/// Result of credential validation (no session created).
/// </summary>
public sealed record CredentialValidationResult
{
    /// <summary>
    /// Whether credentials are valid.
    /// </summary>
    public required bool Succeeded { get; init; }

    /// <summary>
    /// Whether 2FA is required to complete authentication.
    /// </summary>
    public required bool RequiresTwoFactor { get; init; }

    /// <summary>
    /// The user ID if credentials are valid.
    /// </summary>
    public Guid? UserId { get; init; }

    /// <summary>
    /// The username if credentials are valid.
    /// </summary>
    public string? Username { get; init; }

    /// <summary>
    /// The email if credentials are valid.
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    /// Whether the user is anonymous.
    /// </summary>
    public bool IsAnonymous { get; init; }

    /// <summary>
    /// The role codes for this user.
    /// </summary>
    public IReadOnlyCollection<string> Roles { get; init; } = [];

    /// <summary>
    /// Error message if validation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    public static CredentialValidationResult Success(Guid userId, string? username, string? email, IReadOnlyCollection<string> roles, bool isAnonymous = false) => new()
    {
        Succeeded = true,
        RequiresTwoFactor = false,
        UserId = userId,
        Username = username,
        Email = email,
        Roles = roles,
        IsAnonymous = isAnonymous
    };

    public static CredentialValidationResult TwoFactorRequired(Guid userId) => new()
    {
        Succeeded = false,
        RequiresTwoFactor = true,
        UserId = userId
    };

    public static CredentialValidationResult Failed(string? errorMessage = null) => new()
    {
        Succeeded = false,
        RequiresTwoFactor = false,
        ErrorMessage = errorMessage
    };
}

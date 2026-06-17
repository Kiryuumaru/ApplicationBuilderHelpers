namespace Application.Identity.Models;

/// <summary>
/// Result of authentication attempt (may require 2FA).
/// This DTO version returns session as UserSessionDto.
/// </summary>
public sealed record AuthenticationResultDto
{
    /// <summary>
    /// Whether authentication was successful.
    /// </summary>
    public required bool Succeeded { get; init; }

    /// <summary>
    /// Whether 2FA is required to complete authentication.
    /// </summary>
    public required bool RequiresTwoFactor { get; init; }

    /// <summary>
    /// The user ID (always populated for partial success with 2FA required).
    /// </summary>
    public Guid? UserId { get; init; }

    /// <summary>
    /// The user session if authentication succeeded without 2FA.
    /// </summary>
    public UserSessionDto? Session { get; init; }

    /// <summary>
    /// Error message if authentication failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    public static AuthenticationResultDto Success(UserSessionDto session) => new()
    {
        Succeeded = true,
        RequiresTwoFactor = false,
        UserId = session.UserId,
        Session = session
    };

    public static AuthenticationResultDto TwoFactorRequired(Guid userId) => new()
    {
        Succeeded = false,
        RequiresTwoFactor = true,
        UserId = userId
    };

    public static AuthenticationResultDto Failed(string? errorMessage = null) => new()
    {
        Succeeded = false,
        RequiresTwoFactor = false,
        ErrorMessage = errorMessage
    };
}

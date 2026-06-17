using Application.Server.Identity.Enums;

namespace Application.Server.Identity.Models;

/// <summary>
/// Result of post-signature token validation.
/// </summary>
public sealed record TokenValidationResult
{
    /// <summary>
    /// Whether the token passed validation.
    /// </summary>
    public required bool IsValid { get; init; }

    /// <summary>
    /// The type of token that was validated (if successful).
    /// </summary>
    public TokenType? Type { get; init; }

    /// <summary>
    /// Error message (if validation failed).
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static TokenValidationResult Success(TokenType type)
        => new() { IsValid = true, Type = type };

    /// <summary>
    /// Creates a failed validation result.
    /// </summary>
    public static TokenValidationResult Failure(string error)
        => new() { IsValid = false, Error = error };
}

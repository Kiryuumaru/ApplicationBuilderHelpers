using System.ComponentModel.DataAnnotations;

namespace Presentation.WebApi.Controllers.V1.Auth.LoginController.Requests;

/// <summary>
/// Request model for user registration.
/// All fields are optional - an empty body creates an anonymous user.
/// </summary>
public sealed record RegisterRequest
{
    /// <summary>
    /// The desired username for the new account. Optional for anonymous registration.
    /// Required if email or password is provided.
    /// </summary>
    [StringLength(50, MinimumLength = 3)]
    public string? Username { get; init; }

    /// <summary>
    /// The user's email address. Optional.
    /// </summary>
    [EmailAddress]
    public string? Email { get; init; }

    /// <summary>
    /// The desired password for the new account. Optional for anonymous registration.
    /// If provided, ConfirmPassword must also be provided and match.
    /// </summary>
    [StringLength(100, MinimumLength = 8)]
    public string? Password { get; init; }

    /// <summary>
    /// Password confirmation - must match Password. Required if Password is provided.
    /// </summary>
    public string? ConfirmPassword { get; init; }

    /// <summary>
    /// Returns true if all fields are empty (anonymous registration).
    /// </summary>
    public bool IsAnonymous => string.IsNullOrWhiteSpace(Username)
        && string.IsNullOrWhiteSpace(Email)
        && string.IsNullOrWhiteSpace(Password);
}

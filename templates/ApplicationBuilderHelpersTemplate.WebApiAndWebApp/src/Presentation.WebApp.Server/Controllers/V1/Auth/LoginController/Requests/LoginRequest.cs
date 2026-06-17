using System.ComponentModel.DataAnnotations;

namespace Presentation.WebApp.Server.Controllers.V1.Auth.LoginController.Requests;

/// <summary>
/// Request model for user login.
/// </summary>
public sealed record LoginRequest
{
    /// <summary>
    /// The username or email for authentication.
    /// </summary>
    [Required]
    public required string Username { get; init; }

    /// <summary>
    /// The user's password.
    /// </summary>
    [Required]
    public required string Password { get; init; }
}

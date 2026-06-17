#if DEBUG
namespace Presentation.WebApi.Controllers.V1.DevTools.AdminController.Requests;

/// <summary>
/// Request to create an admin user (DEBUG only).
/// </summary>
public sealed class CreateAdminRequest
{
    /// <summary>
    /// Gets or sets the username for the admin user.
    /// </summary>
    public required string Username { get; init; }

    /// <summary>
    /// Gets or sets the email address for the admin user.
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    /// Gets or sets the password for the admin user.
    /// </summary>
    public required string Password { get; init; }
}
#endif

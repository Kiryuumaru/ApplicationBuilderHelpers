using System.ComponentModel.DataAnnotations;

namespace Presentation.WebApi.Controllers.V1.Iam.UsersController.Requests;

/// <summary>
/// Request to update a user's profile.
/// </summary>
public sealed class UpdateUserRequest
{
    /// <summary>
    /// Gets or sets the new email address.
    /// </summary>
    [EmailAddress]
    public string? Email { get; init; }

    /// <summary>
    /// Gets or sets the new phone number.
    /// </summary>
    [Phone]
    public string? PhoneNumber { get; init; }

    /// <summary>
    /// Gets or sets whether to enable or disable lockout.
    /// </summary>
    public bool? LockoutEnabled { get; init; }
}

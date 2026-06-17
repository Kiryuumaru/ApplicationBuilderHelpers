namespace Presentation.WebApp.Server.Controllers.V1.Iam.UsersController.Responses;

/// <summary>
/// Response containing effective permissions for a user.
/// </summary>
public sealed class PermissionsResponse
{
    /// <summary>
    /// Gets or sets the user ID these permissions belong to.
    /// </summary>
    public required Guid UserId { get; init; }

    /// <summary>
    /// Gets or sets the list of effective permission identifiers.
    /// </summary>
    public required IReadOnlyCollection<string> Permissions { get; init; }
}

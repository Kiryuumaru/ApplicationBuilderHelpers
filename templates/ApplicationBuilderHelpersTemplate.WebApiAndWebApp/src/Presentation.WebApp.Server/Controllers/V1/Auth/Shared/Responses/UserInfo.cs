namespace Presentation.WebApp.Server.Controllers.V1.Auth.Shared.Responses;

/// <summary>
/// User information included in authentication responses.
/// </summary>
public sealed record UserInfo
{
    /// <summary>
    /// The user's unique identifier.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// The user's username. Null for anonymous users.
    /// </summary>
    public string? Username { get; init; }

    /// <summary>
    /// The user's email address.
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    /// The user's assigned roles.
    /// </summary>
    public required IReadOnlyCollection<string> Roles { get; init; }

    /// <summary>
    /// The user's effective permissions.
    /// </summary>
    public required IReadOnlyCollection<string> Permissions { get; init; }

    /// <summary>
    /// Whether this is an anonymous (guest) user.
    /// Anonymous users can later link an identity to upgrade to a full account.
    /// </summary>
    public bool IsAnonymous { get; init; }
}

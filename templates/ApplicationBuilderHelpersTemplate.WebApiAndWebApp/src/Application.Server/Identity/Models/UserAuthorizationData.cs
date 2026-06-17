namespace Application.Server.Identity.Models;

/// <summary>
/// Combined authorization data for a user.
/// Used to avoid multiple database queries when generating tokens and user info.
/// </summary>
public sealed record UserAuthorizationData
{
    /// <summary>
    /// The user's unique identifier.
    /// </summary>
    public required Guid UserId { get; init; }

    /// <summary>
    /// The username (null for anonymous users).
    /// </summary>
    public string? Username { get; init; }

    /// <summary>
    /// The user's email (null if not set).
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    /// Whether this is an anonymous user.
    /// </summary>
    public required bool IsAnonymous { get; init; }

    /// <summary>
    /// Formatted role claims with inline parameters (e.g., "USER;roleUserId=abc123").
    /// Ready to be used as JWT role claim values.
    /// </summary>
    public required IReadOnlyCollection<string> FormattedRoles { get; init; }

    /// <summary>
    /// Direct permission grants as scope directives (e.g., "allow;api:custom:read").
    /// Does NOT include role-derived scopes.
    /// </summary>
    public required IReadOnlyCollection<string> DirectPermissionScopes { get; init; }

    /// <summary>
    /// All effective permissions (from roles and direct grants).
    /// Used for display in API responses.
    /// </summary>
    public required IReadOnlyCollection<string> EffectivePermissions { get; init; }
}

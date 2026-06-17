namespace Application.Client.Identity.Models;

/// <summary>
/// Represents the current authentication state of the user.
/// </summary>
public sealed class AuthState
{
    public bool IsAuthenticated { get; init; }
    public Guid UserId { get; init; }
    public string? Username { get; init; }
    public string? Email { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = [];
    public IReadOnlyList<string> Permissions { get; init; } = [];
    public DateTimeOffset? TokenExpiry { get; init; }
    public bool IsAnonymous { get; init; }
    public bool TwoFactorEnabled { get; init; }

    public static AuthState Anonymous => new()
    {
        IsAuthenticated = false,
        IsAnonymous = true,
        UserId = Guid.Empty
    };
}

namespace Application.Client.Identity.Models;

/// <summary>User info included in token responses.</summary>
public sealed class TokenUserInfo
{
    public Guid Id { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }
    public List<string> Roles { get; set; } = [];
    public List<string> Permissions { get; set; } = [];
    public bool IsAnonymous { get; set; }
}

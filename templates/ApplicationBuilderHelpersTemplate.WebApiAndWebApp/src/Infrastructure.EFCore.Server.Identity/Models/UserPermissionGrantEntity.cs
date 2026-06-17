using Domain.Authorization.Enums;

namespace Infrastructure.EFCore.Server.Identity.Models;

internal sealed class UserPermissionGrantEntity
{
    public required Guid UserId { get; set; }
    public required ScopeDirectiveType Type { get; set; }
    public required string PermissionIdentifier { get; set; }
    public string? Description { get; set; }
    public required DateTimeOffset GrantedAt { get; set; }
    public string? GrantedBy { get; set; }
}

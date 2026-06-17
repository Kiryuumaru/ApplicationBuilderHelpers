namespace Infrastructure.EFCore.Server.Identity.Models;

internal sealed class UserLoginEntity
{
    public required string LoginProvider { get; set; }
    public required string ProviderKey { get; set; }
    public required Guid UserId { get; set; }
    public string? ProviderDisplayName { get; set; }
    public string? Email { get; set; }
    public DateTimeOffset LinkedAt { get; set; }
}

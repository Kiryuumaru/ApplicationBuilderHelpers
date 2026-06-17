namespace Infrastructure.EFCore.Identity.Models;

internal sealed class RoleEntity
{
    public Guid Id { get; set; }
    public Guid? RevId { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public required string NormalizedName { get; set; }
    public string? Description { get; set; }
    public string? ScopeTemplatesJson { get; set; }
}

namespace Infrastructure.EFCore.LocalStore.Models;

internal sealed class LocalStoreEntry
{
    public required string Group { get; set; }
    public required string Id { get; set; }
    public string? Data { get; set; }
}

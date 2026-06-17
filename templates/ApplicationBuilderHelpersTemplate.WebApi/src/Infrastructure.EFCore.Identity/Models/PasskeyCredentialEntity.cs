namespace Infrastructure.EFCore.Identity.Models;

internal sealed class PasskeyCredentialEntity
{
    public required Guid Id { get; set; }
    public required Guid UserId { get; set; }
    public required string Name { get; set; }
    public required byte[] CredentialId { get; set; }
    public required byte[] PublicKey { get; set; }
    public required uint SignCount { get; set; }
    public required Guid AaGuid { get; set; }
    public required string CredentialType { get; set; }
    public required DateTimeOffset RegisteredAt { get; set; }
    public DateTimeOffset? LastUsedAt { get; set; }
    public required byte[] UserHandle { get; set; }
    public required string AttestationFormat { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

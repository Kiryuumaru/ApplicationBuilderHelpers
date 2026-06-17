namespace Infrastructure.EFCore.Identity.Models;

internal sealed class UserPasskeyEntity
{
    public required Guid UserId { get; set; }
    public required byte[] CredentialId { get; set; }
    public byte[]? PublicKey { get; set; }
    public string? Name { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public uint SignCount { get; set; }
    public string? Transports { get; set; }
    public bool IsUserVerified { get; set; }
    public bool IsBackupEligible { get; set; }
    public bool IsBackedUp { get; set; }
    public byte[]? AttestationObject { get; set; }
    public byte[]? ClientDataJson { get; set; }
}

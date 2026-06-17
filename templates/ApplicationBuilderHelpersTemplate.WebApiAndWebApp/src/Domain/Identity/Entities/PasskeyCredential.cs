using Domain.Shared.Models;

namespace Domain.Identity.Entities;

public class PasskeyCredential : AuditableEntity
{
    public Guid UserId { get; private set; }
    public string Name { get; private set; }
    public byte[] CredentialId { get; private set; }
    public byte[] PublicKey { get; private set; }
    public uint SignCount { get; private set; }
    public Guid AaGuid { get; private set; }
    public string CredentialType { get; private set; }
    public DateTimeOffset RegisteredAt { get; private set; }
    public DateTimeOffset? LastUsedAt { get; private set; }
    public byte[] UserHandle { get; private set; }
    public string AttestationFormat { get; private set; }

    protected PasskeyCredential(
        Guid id,
        Guid userId,
        string name,
        byte[] credentialId,
        byte[] publicKey,
        uint signCount,
        Guid aaGuid,
        string credentialType,
        byte[] userHandle,
        string attestationFormat,
        DateTimeOffset registeredAt,
        DateTimeOffset? lastUsedAt) : base(id)
    {
        UserId = userId;
        Name = name;
        CredentialId = credentialId;
        PublicKey = publicKey;
        SignCount = signCount;
        AaGuid = aaGuid;
        CredentialType = credentialType;
        UserHandle = userHandle;
        AttestationFormat = attestationFormat;
        RegisteredAt = registeredAt;
        LastUsedAt = lastUsedAt;
    }

    public static PasskeyCredential Create(
        Guid userId,
        string name,
        byte[] credentialId,
        byte[] publicKey,
        uint signCount,
        Guid aaGuid,
        string credentialType,
        byte[] userHandle,
        string attestationFormat)
    {
        ArgumentNullException.ThrowIfNull(credentialId);
        ArgumentNullException.ThrowIfNull(publicKey);
        ArgumentNullException.ThrowIfNull(userHandle);

        return new PasskeyCredential(
            Guid.NewGuid(),
            userId,
            string.IsNullOrWhiteSpace(name) ? "My Passkey" : name,
            credentialId,
            publicKey,
            signCount,
            aaGuid,
            credentialType ?? "public-key",
            userHandle,
            attestationFormat ?? "none",
            DateTimeOffset.UtcNow,
            null);
    }

    public static PasskeyCredential Reconstruct(
        Guid id,
        Guid userId,
        string name,
        byte[] credentialId,
        byte[] publicKey,
        uint signCount,
        Guid aaGuid,
        string credentialType,
        byte[] userHandle,
        string attestationFormat,
        DateTimeOffset registeredAt,
        DateTimeOffset? lastUsedAt)
    {
        return new PasskeyCredential(
            id,
            userId,
            name,
            credentialId,
            publicKey,
            signCount,
            aaGuid,
            credentialType,
            userHandle,
            attestationFormat,
            registeredAt,
            lastUsedAt);
    }

    public void UpdateSignCount(uint newSignCount)
    {
        SignCount = newSignCount;
        LastUsedAt = DateTimeOffset.UtcNow;
        MarkAsModified();
    }

    public void Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Name cannot be empty", nameof(newName));

        Name = newName;
        MarkAsModified();
    }
}

using Domain.Identity.Constants;
using Domain.Identity.Enums;
using Domain.Shared.Models;

namespace Domain.Identity.Entities;

/// <summary>
/// Represents a FIDO2 passkey challenge for registration or authentication.
/// </summary>
public sealed class PasskeyChallenge : Entity
{
    public byte[] Challenge { get; private set; }
    public Guid? UserId { get; private set; }
    public PasskeyChallengeType Type { get; private set; }
    public string OptionsJson { get; private set; }
    public string? CredentialName { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }

    private PasskeyChallenge(
        Guid id,
        byte[] challenge,
        Guid? userId,
        PasskeyChallengeType type,
        string optionsJson,
        string? credentialName,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt) : base(id)
    {
        Challenge = challenge;
        UserId = userId;
        Type = type;
        OptionsJson = optionsJson;
        CredentialName = credentialName;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
    }

    public static PasskeyChallenge Create(
        byte[] challenge,
        Guid? userId,
        PasskeyChallengeType type,
        string optionsJson,
        string? credentialName = null,
        TimeSpan? lifetime = null)
    {
        ArgumentNullException.ThrowIfNull(challenge);
        ArgumentNullException.ThrowIfNull(optionsJson);

        var now = DateTimeOffset.UtcNow;
        var expiry = now.Add(lifetime ?? TokenExpirations.PasskeyChallenge);

        return new PasskeyChallenge(
            Guid.NewGuid(),
            challenge,
            userId,
            type,
            optionsJson,
            credentialName,
            now,
            expiry);
    }

    public bool IsExpired() => DateTimeOffset.UtcNow > ExpiresAt;

    public bool IsValidFor(Guid? userId, PasskeyChallengeType type)
    {
        if (IsExpired())
            return false;

        if (Type != type)
            return false;

        // For registration, userId must match
        if (type == PasskeyChallengeType.Registration && UserId != userId)
            return false;

        return true;
    }

    public static PasskeyChallenge Reconstruct(
        Guid id,
        byte[] challenge,
        Guid? userId,
        PasskeyChallengeType type,
        string optionsJson,
        string? credentialName,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt)
    {
        return new PasskeyChallenge(id, challenge, userId, type, optionsJson, credentialName, createdAt, expiresAt);
    }
}

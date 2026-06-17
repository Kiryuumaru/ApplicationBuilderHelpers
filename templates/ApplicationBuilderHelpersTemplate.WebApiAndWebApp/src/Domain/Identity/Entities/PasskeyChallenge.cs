using Domain.Identity.Constants;
using Domain.Identity.Enums;
using Domain.Shared.Models;

namespace Domain.Identity.Entities;

/// <summary>
/// Represents a temporary challenge used during WebAuthn passkey operations.
/// Challenges are short-lived and should be deleted after use or expiration.
/// </summary>
public class PasskeyChallenge : Entity
{
    /// <summary>
    /// The random challenge bytes (base64url encoded when stored).
    /// </summary>
    public byte[] Challenge { get; private set; }

    /// <summary>
    /// The user ID this challenge is for (null for discoverable credential login).
    /// </summary>
    public Guid? UserId { get; private set; }

    /// <summary>
    /// The type of challenge (Registration or Authentication).
    /// </summary>
    public PasskeyChallengeType Type { get; private set; }

    /// <summary>
    /// The full serialized options JSON returned to the client.
    /// </summary>
    public string OptionsJson { get; private set; }

    /// <summary>
    /// The credential name for registration challenges (null for authentication).
    /// </summary>
    public string? CredentialName { get; private set; }

    /// <summary>
    /// When this challenge was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// When this challenge expires and should no longer be accepted.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; private set; }

    protected PasskeyChallenge(
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

    /// <summary>
    /// Creates a new passkey challenge.
    /// </summary>
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

    /// <summary>
    /// Checks if this challenge has expired.
    /// </summary>
    public bool IsExpired() => DateTimeOffset.UtcNow > ExpiresAt;

    /// <summary>
    /// Checks if this challenge is valid for the given user and type.
    /// </summary>
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

    /// <summary>
    /// Reconstructs a challenge from stored data.
    /// </summary>
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

using Domain.Shared.Exceptions;

namespace Domain.Identity.Exceptions;

/// <summary>
/// Thrown when an API key has been revoked.
/// </summary>
public sealed class ApiKeyRevokedException : DomainException
{
    public ApiKeyRevokedException(Guid keyId)
        : base($"API key with ID '{keyId}' has been revoked.")
    {
        KeyId = keyId;
    }

    public Guid KeyId { get; }
}

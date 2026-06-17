using Domain.Shared.Exceptions;

namespace Domain.Identity.Exceptions;

/// <summary>
/// Thrown when an API key is not found.
/// </summary>
public sealed class ApiKeyNotFoundException : DomainException
{
    public ApiKeyNotFoundException(Guid keyId)
        : base($"API key with ID '{keyId}' was not found.")
    {
        KeyId = keyId;
    }

    public Guid KeyId { get; }
}

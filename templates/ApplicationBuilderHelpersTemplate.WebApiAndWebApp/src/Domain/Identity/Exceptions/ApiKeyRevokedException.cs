namespace Domain.Identity.Exceptions;

public sealed class ApiKeyRevokedException : Exception
{
    public ApiKeyRevokedException(Guid keyId)
        : base($"API key with ID '{keyId}' has been revoked.")
    {
        KeyId = keyId;
    }

    public Guid KeyId { get; }
}

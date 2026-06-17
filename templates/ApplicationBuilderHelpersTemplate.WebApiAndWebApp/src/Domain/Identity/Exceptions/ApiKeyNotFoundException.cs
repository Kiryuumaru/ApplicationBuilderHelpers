namespace Domain.Identity.Exceptions;

public sealed class ApiKeyNotFoundException : Exception
{
    public ApiKeyNotFoundException(Guid keyId)
        : base($"API key with ID '{keyId}' was not found.")
    {
        KeyId = keyId;
    }

    public Guid KeyId { get; }
}

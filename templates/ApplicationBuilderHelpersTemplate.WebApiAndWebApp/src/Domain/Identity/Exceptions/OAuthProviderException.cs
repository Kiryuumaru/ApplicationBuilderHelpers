using Domain.Shared.Exceptions;

namespace Domain.Identity.Exceptions;

public sealed class OAuthProviderException : DomainException
{
    public string? ProviderName { get; }

    public OAuthProviderException(string message)
        : base(message)
    {
    }

    public OAuthProviderException(string message, string providerName)
        : base(message)
    {
        ProviderName = providerName;
    }

    public OAuthProviderException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

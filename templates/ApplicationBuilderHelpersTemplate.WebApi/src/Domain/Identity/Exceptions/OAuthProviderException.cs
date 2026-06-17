using Domain.Shared.Exceptions;

namespace Domain.Identity.Exceptions;

/// <summary>
/// Thrown when an OAuth provider error occurs.
/// </summary>
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

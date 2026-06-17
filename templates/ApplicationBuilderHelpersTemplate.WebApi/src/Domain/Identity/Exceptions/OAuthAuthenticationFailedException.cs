using Domain.Shared.Exceptions;

namespace Domain.Identity.Exceptions;

/// <summary>
/// Thrown when OAuth authentication fails.
/// </summary>
public sealed class OAuthAuthenticationFailedException : DomainException
{
    public OAuthAuthenticationFailedException(string? error, string? errorDescription)
        : base(errorDescription ?? "OAuth authentication was not successful.")
    {
        Error = error;
        ErrorDescription = errorDescription;
    }

    public string? Error { get; }

    public string? ErrorDescription { get; }
}

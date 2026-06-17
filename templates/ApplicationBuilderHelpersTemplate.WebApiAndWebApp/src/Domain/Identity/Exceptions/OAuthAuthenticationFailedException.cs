namespace Domain.Identity.Exceptions;

public sealed class OAuthAuthenticationFailedException : Exception
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

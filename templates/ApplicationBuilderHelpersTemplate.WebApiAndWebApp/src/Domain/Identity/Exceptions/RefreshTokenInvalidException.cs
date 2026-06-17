namespace Domain.Identity.Exceptions;

public sealed class RefreshTokenInvalidException : Exception
{
    public RefreshTokenInvalidException(string? error, string? errorDescription)
        : base(errorDescription ?? "The refresh token is invalid or expired.")
    {
        Error = error;
        ErrorDescription = errorDescription;
    }

    public string? Error { get; }

    public string? ErrorDescription { get; }
}

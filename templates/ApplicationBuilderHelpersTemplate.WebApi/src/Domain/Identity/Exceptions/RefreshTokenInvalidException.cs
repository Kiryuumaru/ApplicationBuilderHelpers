using Domain.Shared.Exceptions;

namespace Domain.Identity.Exceptions;

/// <summary>
/// Thrown when a refresh token is invalid or expired.
/// </summary>
public sealed class RefreshTokenInvalidException : DomainException
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

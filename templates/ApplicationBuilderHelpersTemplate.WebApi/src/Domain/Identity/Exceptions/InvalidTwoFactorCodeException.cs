using Domain.Shared.Exceptions;

namespace Domain.Identity.Exceptions;

/// <summary>
/// Thrown when a two-factor authentication code is invalid.
/// </summary>
public sealed class InvalidTwoFactorCodeException : DomainException
{
    public InvalidTwoFactorCodeException() : base("The 2FA code is invalid or has expired.")
    {
    }

    public InvalidTwoFactorCodeException(string message) : base(message)
    {
    }

    public InvalidTwoFactorCodeException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

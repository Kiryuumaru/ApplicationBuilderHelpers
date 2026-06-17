using Domain.Shared.Exceptions;

namespace Domain.Identity.Exceptions;

/// <summary>
/// Thrown when a two-factor session is invalid or expired.
/// </summary>
public sealed class TwoFactorSessionInvalidException : DomainException
{
    public TwoFactorSessionInvalidException() : base("The user ID is invalid or the 2FA session has expired.")
    {
    }

    public TwoFactorSessionInvalidException(string message) : base(message)
    {
    }

    public TwoFactorSessionInvalidException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

namespace Domain.Identity.Exceptions;

public sealed class InvalidTwoFactorCodeException : Exception
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

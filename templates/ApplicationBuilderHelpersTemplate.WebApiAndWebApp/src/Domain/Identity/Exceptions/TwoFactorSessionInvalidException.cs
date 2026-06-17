namespace Domain.Identity.Exceptions;

public sealed class TwoFactorSessionInvalidException : Exception
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

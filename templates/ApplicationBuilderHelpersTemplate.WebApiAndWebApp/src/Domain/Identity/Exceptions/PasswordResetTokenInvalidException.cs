namespace Domain.Identity.Exceptions;

public sealed class PasswordResetTokenInvalidException : Exception
{
    public PasswordResetTokenInvalidException()
        : base("The password reset token is invalid or has expired.")
    {
    }

    public PasswordResetTokenInvalidException(string message) : base(message)
    {
    }

    public PasswordResetTokenInvalidException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

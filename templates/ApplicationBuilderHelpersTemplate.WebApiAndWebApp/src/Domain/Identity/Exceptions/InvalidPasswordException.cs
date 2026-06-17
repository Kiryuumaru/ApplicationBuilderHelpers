namespace Domain.Identity.Exceptions;

public sealed class InvalidPasswordException : Exception
{
    public InvalidPasswordException() : base("The current password is incorrect.")
    {
    }

    public InvalidPasswordException(string message) : base(message)
    {
    }

    public InvalidPasswordException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

using Domain.Shared.Exceptions;

namespace Domain.Identity.Exceptions;

/// <summary>
/// Thrown when a provided password is incorrect.
/// </summary>
public sealed class InvalidPasswordException : DomainException
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

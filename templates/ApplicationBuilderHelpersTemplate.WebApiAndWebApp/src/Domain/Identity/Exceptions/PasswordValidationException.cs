using Domain.Shared.Exceptions;

namespace Domain.Identity.Exceptions;

public sealed class PasswordValidationException : DomainException
{
    public PasswordValidationException(string message)
        : base(message)
    {
    }

    public PasswordValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

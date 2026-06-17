using Domain.Shared.Exceptions;

namespace Domain.Identity.Exceptions;

/// <summary>
/// Thrown when a password fails validation requirements.
/// </summary>
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

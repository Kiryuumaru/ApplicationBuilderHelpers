using Domain.Shared.Exceptions;

namespace Domain.Identity.Exceptions;

/// <summary>
/// Thrown when a password reset token is invalid or expired.
/// </summary>
public sealed class PasswordResetTokenInvalidException : DomainException
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

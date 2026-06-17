using Domain.Shared.Exceptions;

namespace Domain.Identity.Exceptions;

/// <summary>
/// Thrown when a two-factor authentication operation fails.
/// </summary>
public sealed class TwoFactorException : DomainException
{
    public TwoFactorException(string message)
        : base(message)
    {
    }

    public TwoFactorException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

using Domain.Shared.Exceptions;

namespace Domain.Identity.Exceptions;

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

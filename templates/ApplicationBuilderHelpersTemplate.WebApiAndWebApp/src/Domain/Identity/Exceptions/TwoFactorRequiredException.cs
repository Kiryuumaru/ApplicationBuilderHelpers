using Domain.Shared.Exceptions;

namespace Domain.Identity.Exceptions;

public sealed class TwoFactorRequiredException : DomainException
{
    public Guid UserId { get; }

    public TwoFactorRequiredException(Guid userId)
        : base("Two-factor authentication is required.")
    {
        UserId = userId;
    }

    public TwoFactorRequiredException(Guid userId, string message)
        : base(message)
    {
        UserId = userId;
    }
}

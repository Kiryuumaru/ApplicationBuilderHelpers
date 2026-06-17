using Domain.Shared.Exceptions;

namespace Domain.Identity.Exceptions;

/// <summary>
/// Thrown when two-factor authentication is required but not completed.
/// </summary>
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

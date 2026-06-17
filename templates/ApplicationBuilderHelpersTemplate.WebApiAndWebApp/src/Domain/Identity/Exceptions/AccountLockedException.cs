using Domain.Shared.Exceptions;

namespace Domain.Identity.Exceptions;

public sealed class AccountLockedException : DomainException
{
    public DateTimeOffset? LockoutEnd { get; }

    public AccountLockedException(string message, DateTimeOffset? lockoutEnd = null) : base(message)
    {
        LockoutEnd = lockoutEnd;
    }

    public AccountLockedException(DateTimeOffset lockoutEnd)
        : base($"Account is locked until {lockoutEnd:u}.")
    {
        LockoutEnd = lockoutEnd;
    }
}

using Domain.Shared.Exceptions;

namespace Domain.Identity.Exceptions;

public sealed class AuthenticationException(string message) : DomainException(message)
{
}

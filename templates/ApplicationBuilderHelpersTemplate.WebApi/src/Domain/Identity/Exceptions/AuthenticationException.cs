using Domain.Shared.Exceptions;

namespace Domain.Identity.Exceptions;

/// <summary>
/// Thrown when user authentication fails.
/// </summary>
public sealed class AuthenticationException(string message) : DomainException(message)
{
}

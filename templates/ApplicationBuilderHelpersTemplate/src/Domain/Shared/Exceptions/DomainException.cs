namespace Domain.Shared.Exceptions;

/// <summary>
/// Base exception for all domain-level exceptions.
/// Derive from this class for specific domain error conditions.
/// </summary>
public class DomainException : Exception
{
    public DomainException()
    {
    }

    public DomainException(string message) : base(message)
    {
    }

    public DomainException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

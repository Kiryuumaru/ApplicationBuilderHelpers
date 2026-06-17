namespace Domain.Shared.Exceptions;

public sealed class ValidationException : DomainException
{
    public string? PropertyName { get; }

    public ValidationException(string message)
        : base(message)
    {
    }

    public ValidationException(string propertyName, string message)
        : base(message)
    {
        PropertyName = propertyName;
    }

    public ValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

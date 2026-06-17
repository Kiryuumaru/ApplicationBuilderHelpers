namespace Domain.Shared.Exceptions;

public sealed class EntityNotFoundException : DomainException
{
    public string? EntityType { get; }
    public string? EntityIdentifier { get; }

    public EntityNotFoundException(string message)
        : base(message)
    {
    }

    public EntityNotFoundException(string entityType, string entityIdentifier)
        : base($"{entityType} '{entityIdentifier}' was not found.")
    {
        EntityType = entityType;
        EntityIdentifier = entityIdentifier;
    }

    public EntityNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

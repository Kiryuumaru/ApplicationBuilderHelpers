namespace Domain.Shared.Exceptions;

/// <summary>
/// Thrown when a reserved name is used.
/// </summary>
public sealed class ReservedNameException : DomainException
{
    public string? ReservedName { get; }

    public ReservedNameException(string message)
        : base(message)
    {
    }

    public ReservedNameException(string reservedName, string entityType)
        : base($"The name '{reservedName}' is reserved and cannot be used for {entityType}.")
    {
        ReservedName = reservedName;
    }

    public ReservedNameException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

namespace Domain.Shared.Interfaces;

/// <summary>
/// Marker interface for domain events raised by aggregate roots.
/// </summary>
public interface IDomainEvent
{
    Guid Id { get; }

    DateTimeOffset OccurredOn { get; }
}

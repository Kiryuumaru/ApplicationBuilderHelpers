namespace Domain.Shared.Interfaces;

public interface IDomainEvent
{
    Guid Id { get; }

    DateTimeOffset OccurredOn { get; }
}

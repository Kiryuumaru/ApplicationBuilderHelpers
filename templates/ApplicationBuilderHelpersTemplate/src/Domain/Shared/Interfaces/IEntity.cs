namespace Domain.Shared.Interfaces;

/// <summary>
/// Marks tracked entities with unique identity and revision tracking.
/// </summary>
public interface IEntity
{
    Guid Id { get; }

    Guid RevId { get; }
}

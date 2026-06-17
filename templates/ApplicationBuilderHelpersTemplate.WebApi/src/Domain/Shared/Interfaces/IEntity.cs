namespace Domain.Shared.Interfaces;

/// <summary>
/// Marker interface for domain entities with identity.
/// </summary>
public interface IEntity
{
    Guid Id { get; }
}

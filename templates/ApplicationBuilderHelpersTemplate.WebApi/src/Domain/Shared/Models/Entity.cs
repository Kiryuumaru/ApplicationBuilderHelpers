using Domain.Shared.Interfaces;

namespace Domain.Shared.Models;

/// <summary>
/// Base class for entities with unique identity.
/// </summary>
public abstract class Entity : DomainObject, IEntity
{
    public Guid Id { get; private set; }
    public Guid RevId { get; protected set; } = Guid.NewGuid();

    protected Entity(Guid id)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Id cannot be empty", nameof(id));
        }

        Id = id;
    }
}

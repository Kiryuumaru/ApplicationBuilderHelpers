using Domain.Shared.Interfaces;

namespace Domain.Shared.Models;

public abstract class Entity : DomainObject, IEntity
{
    public Guid Id { get; private set; }
    public Guid RevId { get; protected set; } = Guid.NewGuid();

    protected Entity(Guid id)
    {
        Id = id;
    }
}

using Domain.Shared.Interfaces;

namespace Domain.Shared.Models;

public abstract class Entity : IEntity
{
    public Guid Id { get; private set; }
    public Guid RevId { get; private set; } = Guid.NewGuid();

    protected void UpdateRevision() => RevId = Guid.NewGuid();

    protected Entity(Guid id)
    {
        Id = id;
    }
}

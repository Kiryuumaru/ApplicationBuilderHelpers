namespace Domain.Shared.Models;

public abstract class AuditableEntity(Guid id) : Entity(id)
{
    public DateTimeOffset Created { get; private set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset LastModified { get; private set; } = DateTimeOffset.UtcNow;

    protected void MarkAsModified()
    {
        LastModified = DateTimeOffset.UtcNow;
        RevId = Guid.NewGuid();
    }
}

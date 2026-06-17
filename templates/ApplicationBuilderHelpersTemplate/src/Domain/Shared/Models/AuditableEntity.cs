namespace Domain.Shared.Models;

public abstract class AuditableEntity : Entity
{
    public DateTimeOffset Created { get; private set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset LastModified { get; private set; } = DateTimeOffset.UtcNow;

    protected AuditableEntity(Guid id) : base(id)
    {
    }

    protected void MarkAsModified()
    {
        LastModified = DateTimeOffset.UtcNow;
        UpdateRevision();
    }
}

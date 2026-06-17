using Domain.Shared.Models;

namespace Domain.UnitTests.Shared.Models;

public class EntityTests
{
    private sealed class TestEntity : Entity
    {
        public TestEntity(Guid id) : base(id)
        {
        }

        public void TriggerUpdateRevision() => UpdateRevision();
    }

    [Fact]
    public void Constructor_WithId_SetsId()
    {
        var id = Guid.NewGuid();

        var entity = new TestEntity(id);

        Assert.Equal(id, entity.Id);
    }

    [Fact]
    public void Constructor_SetsInitialRevId()
    {
        var entity = new TestEntity(Guid.NewGuid());

        Assert.NotEqual(Guid.Empty, entity.RevId);
    }

    [Fact]
    public void UpdateRevision_ChangesRevId()
    {
        var entity = new TestEntity(Guid.NewGuid());
        var originalRevId = entity.RevId;

        entity.TriggerUpdateRevision();

        Assert.NotEqual(originalRevId, entity.RevId);
    }

    [Fact]
    public void UpdateRevision_GeneratesUniqueRevIds()
    {
        var entity = new TestEntity(Guid.NewGuid());

        entity.TriggerUpdateRevision();
        var rev1 = entity.RevId;

        entity.TriggerUpdateRevision();
        var rev2 = entity.RevId;

        Assert.NotEqual(rev1, rev2);
    }
}

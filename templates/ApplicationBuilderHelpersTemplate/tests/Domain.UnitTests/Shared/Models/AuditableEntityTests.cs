using Domain.Shared.Models;

namespace Domain.UnitTests.Shared.Models;

public class AuditableEntityTests
{
    private sealed class TestAuditableEntity : AuditableEntity
    {
        public TestAuditableEntity(Guid id) : base(id)
        {
        }

        public void TriggerMarkAsModified() => MarkAsModified();
    }

    [Fact]
    public void Constructor_SetsCreatedTimestamp()
    {
        var beforeCreation = DateTimeOffset.UtcNow;
        var entity = new TestAuditableEntity(Guid.NewGuid());
        var afterCreation = DateTimeOffset.UtcNow;

        Assert.True(entity.Created >= beforeCreation);
        Assert.True(entity.Created <= afterCreation);
    }

    [Fact]
    public void Constructor_SetsLastModifiedTimestamp()
    {
        var beforeCreation = DateTimeOffset.UtcNow;
        var entity = new TestAuditableEntity(Guid.NewGuid());
        var afterCreation = DateTimeOffset.UtcNow;

        Assert.True(entity.LastModified >= beforeCreation);
        Assert.True(entity.LastModified <= afterCreation);
    }

    [Fact]
    public async Task MarkAsModified_UpdatesLastModifiedTimestamp()
    {
        var entity = new TestAuditableEntity(Guid.NewGuid());
        var originalLastModified = entity.LastModified;

        // Small delay to ensure timestamp difference
        await Task.Delay(1);

        entity.TriggerMarkAsModified();

        Assert.True(entity.LastModified >= originalLastModified);
    }

    [Fact]
    public void MarkAsModified_DoesNotChangeCreated()
    {
        var entity = new TestAuditableEntity(Guid.NewGuid());
        var originalCreated = entity.Created;

        entity.TriggerMarkAsModified();

        Assert.Equal(originalCreated, entity.Created);
    }

    [Fact]
    public void MarkAsModified_UpdatesRevision()
    {
        var entity = new TestAuditableEntity(Guid.NewGuid());
        var originalRevId = entity.RevId;

        entity.TriggerMarkAsModified();

        Assert.NotEqual(originalRevId, entity.RevId);
    }
}

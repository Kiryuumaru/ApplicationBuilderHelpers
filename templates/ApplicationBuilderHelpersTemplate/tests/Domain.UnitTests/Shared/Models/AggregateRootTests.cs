using Domain.Shared.Interfaces;
using Domain.Shared.Models;

namespace Domain.UnitTests.Shared.Models;

public class AggregateRootTests
{
    private sealed record TestDomainEvent(string Value) : DomainEvent;

    private sealed class TestAggregateRoot : AggregateRoot
    {
        public TestAggregateRoot(Guid id) : base(id)
        {
        }

        public void RaiseEvent(IDomainEvent domainEvent) => AddDomainEvent(domainEvent);

        public void DropEvent(IDomainEvent domainEvent) => RemoveDomainEvent(domainEvent);
    }

    [Fact]
    public void Constructor_InitializesEmptyDomainEvents()
    {
        var aggregate = new TestAggregateRoot(Guid.NewGuid());

        Assert.Empty(aggregate.DomainEvents);
    }

    [Fact]
    public void AddDomainEvent_AddsEventToCollection()
    {
        var aggregate = new TestAggregateRoot(Guid.NewGuid());
        var domainEvent = new TestDomainEvent("test");

        aggregate.RaiseEvent(domainEvent);

        Assert.Single(aggregate.DomainEvents);
        Assert.Contains(domainEvent, aggregate.DomainEvents);
    }

    [Fact]
    public void AddDomainEvent_AllowsMultipleEvents()
    {
        var aggregate = new TestAggregateRoot(Guid.NewGuid());
        var event1 = new TestDomainEvent("test1");
        var event2 = new TestDomainEvent("test2");

        aggregate.RaiseEvent(event1);
        aggregate.RaiseEvent(event2);

        Assert.Equal(2, aggregate.DomainEvents.Count);
    }

    [Fact]
    public void RemoveDomainEvent_RemovesEventFromCollection()
    {
        var aggregate = new TestAggregateRoot(Guid.NewGuid());
        var domainEvent = new TestDomainEvent("test");
        aggregate.RaiseEvent(domainEvent);

        aggregate.DropEvent(domainEvent);

        Assert.Empty(aggregate.DomainEvents);
    }

    [Fact]
    public void ClearDomainEvents_RemovesAllEvents()
    {
        var aggregate = new TestAggregateRoot(Guid.NewGuid());
        aggregate.RaiseEvent(new TestDomainEvent("test1"));
        aggregate.RaiseEvent(new TestDomainEvent("test2"));

        aggregate.ClearDomainEvents();

        Assert.Empty(aggregate.DomainEvents);
    }

    [Fact]
    public void DomainEvents_ReturnsReadOnlyCollection()
    {
        var aggregate = new TestAggregateRoot(Guid.NewGuid());
        aggregate.RaiseEvent(new TestDomainEvent("test"));

        var events = aggregate.DomainEvents;

        Assert.IsAssignableFrom<IReadOnlyCollection<IDomainEvent>>(events);
    }

    [Fact]
    public void AggregateRoot_ImplementsIAggregateRoot()
    {
        var aggregate = new TestAggregateRoot(Guid.NewGuid());

        Assert.IsAssignableFrom<IAggregateRoot>(aggregate);
    }
}

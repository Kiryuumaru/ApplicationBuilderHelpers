using Application.Shared.Interfaces;
using Application.Shared.Models;
using Domain.Shared.Interfaces;
using Domain.Shared.Models;

namespace Application.UnitTests.Shared.Models;

public class DomainEventHandlerTests
{
    private sealed record TestEvent(string Value) : DomainEvent;
    private sealed record OtherEvent(int Number) : DomainEvent;

    private sealed class TestEventHandler : DomainEventHandler<TestEvent>
    {
        public List<TestEvent> HandledEvents { get; } = [];

        public override ValueTask HandleAsync(TestEvent domainEvent, CancellationToken cancellationToken = default)
        {
            HandledEvents.Add(domainEvent);
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public void CanHandle_WithMatchingEventType_ReturnsTrue()
    {
        var handler = new TestEventHandler();
        var domainEvent = new TestEvent("test");

        var result = handler.CanHandle(domainEvent);

        Assert.True(result);
    }

    [Fact]
    public void CanHandle_WithNonMatchingEventType_ReturnsFalse()
    {
        var handler = new TestEventHandler();
        var domainEvent = new OtherEvent(42);

        var result = handler.CanHandle(domainEvent);

        Assert.False(result);
    }

    [Fact]
    public async Task HandleAsync_WithMatchingEventType_CallsTypedHandler()
    {
        var handler = new TestEventHandler();
        var domainEvent = new TestEvent("test");

        await handler.HandleAsync((IDomainEvent)domainEvent);

        Assert.Single(handler.HandledEvents);
        Assert.Equal(domainEvent, handler.HandledEvents[0]);
    }

    [Fact]
    public async Task HandleAsync_WithNonMatchingEventType_DoesNotCallTypedHandler()
    {
        var handler = new TestEventHandler();
        var domainEvent = new OtherEvent(42);

        await handler.HandleAsync(domainEvent);

        Assert.Empty(handler.HandledEvents);
    }

    [Fact]
    public void Handler_ImplementsIDomainEventHandler()
    {
        var handler = new TestEventHandler();

        Assert.IsAssignableFrom<IDomainEventHandler>(handler);
        Assert.IsAssignableFrom<IDomainEventHandler<TestEvent>>(handler);
    }
}

using Application.Shared.Interfaces;
using Application.Shared.Services;
using Domain.Shared.Interfaces;
using Domain.Shared.Models;

namespace Application.UnitTests.Shared.Services;

public class DomainEventDispatcherTests
{
    private sealed record TestEvent(string Value) : DomainEvent;
    private sealed record OtherEvent(int Number) : DomainEvent;

    [Fact]
    public async Task DispatchAsync_WithMatchingHandler_CallsHandler()
    {
        var handler = Substitute.For<IDomainEventHandler>();
        handler.CanHandle(Arg.Any<TestEvent>()).Returns(true);
        handler.HandleAsync(Arg.Any<IDomainEvent>(), Arg.Any<CancellationToken>()).Returns(ValueTask.CompletedTask);

        var dispatcher = new DomainEventDispatcher([handler]);
        var domainEvent = new TestEvent("test");

        await dispatcher.DispatchAsync(domainEvent);

        await handler.Received(1).HandleAsync(domainEvent, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchAsync_WithNonMatchingHandler_SkipsHandler()
    {
        var handler = Substitute.For<IDomainEventHandler>();
        handler.CanHandle(Arg.Any<TestEvent>()).Returns(false);

        var dispatcher = new DomainEventDispatcher([handler]);
        var domainEvent = new TestEvent("test");

        await dispatcher.DispatchAsync(domainEvent);

        await handler.DidNotReceive().HandleAsync(Arg.Any<IDomainEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchAsync_WithMultipleHandlers_CallsAllMatchingHandlers()
    {
        var handler1 = Substitute.For<IDomainEventHandler>();
        handler1.CanHandle(Arg.Any<TestEvent>()).Returns(true);
        handler1.HandleAsync(Arg.Any<IDomainEvent>(), Arg.Any<CancellationToken>()).Returns(ValueTask.CompletedTask);

        var handler2 = Substitute.For<IDomainEventHandler>();
        handler2.CanHandle(Arg.Any<TestEvent>()).Returns(true);
        handler2.HandleAsync(Arg.Any<IDomainEvent>(), Arg.Any<CancellationToken>()).Returns(ValueTask.CompletedTask);

        var dispatcher = new DomainEventDispatcher([handler1, handler2]);
        var domainEvent = new TestEvent("test");

        await dispatcher.DispatchAsync(domainEvent);

        await handler1.Received(1).HandleAsync(domainEvent, Arg.Any<CancellationToken>());
        await handler2.Received(1).HandleAsync(domainEvent, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchAsync_WithNoHandlers_CompletesSuccessfully()
    {
        var dispatcher = new DomainEventDispatcher([]);
        var domainEvent = new TestEvent("test");

        await dispatcher.DispatchAsync(domainEvent);
    }

    [Fact]
    public async Task DispatchAsync_WithMultipleEvents_DispatchesAllEvents()
    {
        var handler = Substitute.For<IDomainEventHandler>();
        handler.CanHandle(Arg.Any<TestEvent>()).Returns(true);
        handler.HandleAsync(Arg.Any<IDomainEvent>(), Arg.Any<CancellationToken>()).Returns(ValueTask.CompletedTask);

        var dispatcher = new DomainEventDispatcher([handler]);
        var events = new List<IDomainEvent>
        {
            new TestEvent("test1"),
            new TestEvent("test2"),
            new TestEvent("test3")
        };

        await dispatcher.DispatchAsync(events);

        await handler.Received(3).HandleAsync(Arg.Any<IDomainEvent>(), Arg.Any<CancellationToken>());
    }
}

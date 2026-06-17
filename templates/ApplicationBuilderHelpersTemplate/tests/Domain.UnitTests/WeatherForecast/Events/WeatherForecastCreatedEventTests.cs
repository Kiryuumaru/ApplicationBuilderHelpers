using Domain.Shared.Models;
using Domain.WeatherForecast.Events;

namespace Domain.UnitTests.WeatherForecast.Events;

public class WeatherForecastCreatedEventTests
{
    [Fact]
    public void Constructor_WithValidParameters_CreatesEvent()
    {
        var entityId = Guid.NewGuid();
        var forecastDate = new DateOnly(2026, 4, 27);

        var domainEvent = new WeatherForecastCreatedEvent(entityId, "Tokyo", forecastDate);

        Assert.Equal(entityId, domainEvent.EntityId);
        Assert.Equal("Tokyo", domainEvent.Location);
        Assert.Equal(forecastDate, domainEvent.ForecastDate);
    }

    [Fact]
    public void Event_HasUniqueId()
    {
        var event1 = new WeatherForecastCreatedEvent(Guid.NewGuid(), "NYC", new DateOnly(2026, 4, 27));
        var event2 = new WeatherForecastCreatedEvent(Guid.NewGuid(), "NYC", new DateOnly(2026, 4, 27));

        Assert.NotEqual(event1.Id, event2.Id);
    }

    [Fact]
    public void Event_HasOccurredOnTimestamp()
    {
        var beforeCreation = DateTimeOffset.UtcNow;
        var domainEvent = new WeatherForecastCreatedEvent(Guid.NewGuid(), "London", new DateOnly(2026, 5, 1));
        var afterCreation = DateTimeOffset.UtcNow;

        Assert.True(domainEvent.OccurredOn >= beforeCreation);
        Assert.True(domainEvent.OccurredOn <= afterCreation);
    }

    [Fact]
    public void Event_InheritsFromDomainEvent()
    {
        var domainEvent = new WeatherForecastCreatedEvent(Guid.NewGuid(), "Paris", new DateOnly(2026, 6, 15));

        Assert.IsAssignableFrom<DomainEvent>(domainEvent);
    }
}

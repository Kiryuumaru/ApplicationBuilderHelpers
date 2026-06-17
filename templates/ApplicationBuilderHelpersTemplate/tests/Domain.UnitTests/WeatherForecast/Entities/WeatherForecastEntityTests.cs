using Domain.WeatherForecast.Entities;
using Domain.WeatherForecast.Enums;
using Domain.WeatherForecast.Events;
using Domain.WeatherForecast.ValueObjects;

namespace Domain.UnitTests.WeatherForecast.Entities;

public class WeatherForecastEntityTests
{
    [Fact]
    public void Create_WithValidParameters_CreatesEntity()
    {
        var high = Temperature.Create(25.0);
        var low = Temperature.Create(15.0);

        var entity = WeatherForecastEntity.Create(
            "New York",
            new DateOnly(2026, 4, 27),
            high,
            low,
            WeatherCondition.Sunny,
            "Clear skies");

        Assert.NotEqual(Guid.Empty, entity.Id);
        Assert.Equal("New York", entity.Location);
        Assert.Equal(new DateOnly(2026, 4, 27), entity.ForecastDate);
        Assert.Equal(25.0, entity.HighTemperature.Celsius);
        Assert.Equal(15.0, entity.LowTemperature.Celsius);
        Assert.Equal(WeatherCondition.Sunny, entity.Condition);
        Assert.Equal("Clear skies", entity.Summary);
    }

    [Fact]
    public void Create_WithNullLocation_ThrowsArgumentNullException()
    {
        var high = Temperature.Create(25.0);
        var low = Temperature.Create(15.0);

        Assert.Throws<ArgumentNullException>(() =>
            WeatherForecastEntity.Create(null!, new DateOnly(2026, 4, 27), high, low, WeatherCondition.Sunny, "Clear"));
    }

    [Fact]
    public void Create_WithNullHighTemperature_ThrowsArgumentNullException()
    {
        var low = Temperature.Create(15.0);

        Assert.Throws<ArgumentNullException>(() =>
            WeatherForecastEntity.Create("NYC", new DateOnly(2026, 4, 27), null!, low, WeatherCondition.Sunny, "Clear"));
    }

    [Fact]
    public void Create_WithNullLowTemperature_ThrowsArgumentNullException()
    {
        var high = Temperature.Create(25.0);

        Assert.Throws<ArgumentNullException>(() =>
            WeatherForecastEntity.Create("NYC", new DateOnly(2026, 4, 27), high, null!, WeatherCondition.Sunny, "Clear"));
    }

    [Fact]
    public void Create_WithNullSummary_ThrowsArgumentNullException()
    {
        var high = Temperature.Create(25.0);
        var low = Temperature.Create(15.0);

        Assert.Throws<ArgumentNullException>(() =>
            WeatherForecastEntity.Create("NYC", new DateOnly(2026, 4, 27), high, low, WeatherCondition.Sunny, null!));
    }

    [Fact]
    public void Create_WithHighLowerThanLow_ThrowsArgumentException()
    {
        var high = Temperature.Create(10.0);
        var low = Temperature.Create(20.0);

        Assert.Throws<ArgumentException>(() =>
            WeatherForecastEntity.Create("NYC", new DateOnly(2026, 4, 27), high, low, WeatherCondition.Sunny, "Clear"));
    }

    [Fact]
    public void Create_RaisesWeatherForecastCreatedEvent()
    {
        var high = Temperature.Create(30.0);
        var low = Temperature.Create(20.0);

        var entity = WeatherForecastEntity.Create(
            "London",
            new DateOnly(2026, 5, 1),
            high,
            low,
            WeatherCondition.Cloudy,
            "Overcast");

        Assert.Single(entity.DomainEvents);
        var domainEvent = Assert.IsType<WeatherForecastCreatedEvent>(entity.DomainEvents.First());
        Assert.Equal(entity.Id, domainEvent.EntityId);
        Assert.Equal("London", domainEvent.Location);
        Assert.Equal(new DateOnly(2026, 5, 1), domainEvent.ForecastDate);
    }

    [Fact]
    public void ClearDomainEvents_RemovesAllEvents()
    {
        var high = Temperature.Create(25.0);
        var low = Temperature.Create(15.0);

        var entity = WeatherForecastEntity.Create("NYC", new DateOnly(2026, 4, 27), high, low, WeatherCondition.Sunny, "Clear");
        entity.ClearDomainEvents();

        Assert.Empty(entity.DomainEvents);
    }

    [Fact]
    public void Create_WithEqualHighAndLow_Succeeds()
    {
        var temp = Temperature.Create(20.0);

        var entity = WeatherForecastEntity.Create(
            "Miami",
            new DateOnly(2026, 6, 1),
            temp,
            temp,
            WeatherCondition.Sunny,
            "Stable temperature");

        Assert.Equal(20.0, entity.HighTemperature.Celsius);
        Assert.Equal(20.0, entity.LowTemperature.Celsius);
    }
}

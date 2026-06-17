using Domain.Shared.Models;
using Domain.WeatherForecast.Enums;
using Domain.WeatherForecast.Events;
using Domain.WeatherForecast.ValueObjects;

namespace Domain.WeatherForecast.Entities;

/// <summary>
/// Aggregate root representing a weather forecast for a specific location and date.
/// </summary>
public class WeatherForecastEntity : AggregateRoot
{
    public string Location { get; private set; }

    public DateOnly ForecastDate { get; private set; }

    public Temperature HighTemperature { get; private set; }

    public Temperature LowTemperature { get; private set; }

    public WeatherCondition Condition { get; private set; }

    public string Summary { get; private set; }

    protected WeatherForecastEntity(
        Guid id,
        string location,
        DateOnly forecastDate,
        Temperature highTemperature,
        Temperature lowTemperature,
        WeatherCondition condition,
        string summary) : base(id)
    {
        Location = location;
        ForecastDate = forecastDate;
        HighTemperature = highTemperature;
        LowTemperature = lowTemperature;
        Condition = condition;
        Summary = summary;
    }

    public static WeatherForecastEntity Create(
        string location,
        DateOnly forecastDate,
        Temperature highTemperature,
        Temperature lowTemperature,
        WeatherCondition condition,
        string summary)
    {
        ArgumentNullException.ThrowIfNull(location);
        ArgumentNullException.ThrowIfNull(highTemperature);
        ArgumentNullException.ThrowIfNull(lowTemperature);
        ArgumentNullException.ThrowIfNull(summary);

        if (highTemperature.Celsius < lowTemperature.Celsius)
            throw new ArgumentException("High temperature cannot be lower than low temperature.", nameof(highTemperature));

        var entity = new WeatherForecastEntity(
            Guid.NewGuid(),
            location,
            forecastDate,
            highTemperature,
            lowTemperature,
            condition,
            summary);

        entity.AddDomainEvent(new WeatherForecastCreatedEvent(entity.Id, entity.Location, entity.ForecastDate));
        return entity;
    }
}

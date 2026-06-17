using Domain.Shared.Models;

namespace Domain.WeatherForecast.ValueObjects;

/// <summary>
/// Represents a temperature measurement with Celsius value and Fahrenheit conversion.
/// </summary>
public class Temperature : ValueObject
{
    public double Celsius { get; }

    public double Fahrenheit => Celsius * 9.0 / 5.0 + 32.0;

    protected Temperature(double celsius)
    {
        Celsius = celsius;
    }

    public static Temperature Create(double celsius)
    {
        if (celsius < -273.15)
            throw new ArgumentOutOfRangeException(nameof(celsius), "Temperature cannot be below absolute zero (-273.15°C).");

        return new Temperature(celsius);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Celsius;
    }
}

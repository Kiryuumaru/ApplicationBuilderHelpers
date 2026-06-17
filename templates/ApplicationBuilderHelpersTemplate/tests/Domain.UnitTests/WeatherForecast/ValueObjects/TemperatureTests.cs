using Domain.WeatherForecast.ValueObjects;

namespace Domain.UnitTests.WeatherForecast.ValueObjects;

public class TemperatureTests
{
    [Fact]
    public void Create_WithValidCelsius_CreatesTemperature()
    {
        var temp = Temperature.Create(25.5);

        Assert.Equal(25.5, temp.Celsius);
    }

    [Fact]
    public void Create_WithZeroCelsius_CreatesTemperature()
    {
        var temp = Temperature.Create(0.0);

        Assert.Equal(0.0, temp.Celsius);
    }

    [Fact]
    public void Create_WithNegativeCelsius_CreatesTemperature()
    {
        var temp = Temperature.Create(-40.0);

        Assert.Equal(-40.0, temp.Celsius);
    }

    [Fact]
    public void Create_BelowAbsoluteZero_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Temperature.Create(-273.16));
    }

    [Fact]
    public void Create_AtAbsoluteZero_Succeeds()
    {
        var temp = Temperature.Create(-273.15);

        Assert.Equal(-273.15, temp.Celsius);
    }

    [Fact]
    public void Fahrenheit_ConvertsCorrectly_ForFreezing()
    {
        var temp = Temperature.Create(0.0);

        Assert.Equal(32.0, temp.Fahrenheit);
    }

    [Fact]
    public void Fahrenheit_ConvertsCorrectly_ForBoiling()
    {
        var temp = Temperature.Create(100.0);

        Assert.Equal(212.0, temp.Fahrenheit);
    }

    [Fact]
    public void Fahrenheit_ConvertsCorrectly_ForBodyTemp()
    {
        var temp = Temperature.Create(37.0);

        Assert.Equal(98.6, temp.Fahrenheit, 1);
    }

    [Fact]
    public void Equals_WithSameValues_ReturnsTrue()
    {
        var temp1 = Temperature.Create(25.0);
        var temp2 = Temperature.Create(25.0);

        Assert.Equal(temp1, temp2);
    }

    [Fact]
    public void Equals_WithDifferentValues_ReturnsFalse()
    {
        var temp1 = Temperature.Create(25.0);
        var temp2 = Temperature.Create(30.0);

        Assert.NotEqual(temp1, temp2);
    }

    [Fact]
    public void GetHashCode_WithSameValues_ReturnsSameHash()
    {
        var temp1 = Temperature.Create(25.0);
        var temp2 = Temperature.Create(25.0);

        Assert.Equal(temp1.GetHashCode(), temp2.GetHashCode());
    }

    [Fact]
    public void EqualityOperator_WithSameValues_ReturnsTrue()
    {
        var temp1 = Temperature.Create(25.0);
        var temp2 = Temperature.Create(25.0);

        Assert.True(temp1 == temp2);
    }

    [Fact]
    public void InequalityOperator_WithDifferentValues_ReturnsTrue()
    {
        var temp1 = Temperature.Create(25.0);
        var temp2 = Temperature.Create(30.0);

        Assert.True(temp1 != temp2);
    }
}

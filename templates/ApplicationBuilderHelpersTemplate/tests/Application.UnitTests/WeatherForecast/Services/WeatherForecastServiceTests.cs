using Application.WeatherForecast.Services;
using Domain.WeatherForecast.Entities;
using Domain.WeatherForecast.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;

namespace Application.UnitTests.WeatherForecast.Services;

public class WeatherForecastServiceTests
{
    private readonly IWeatherForecastRepository _repository;
    private readonly IWeatherForecastUnitOfWork _unitOfWork;
    private readonly WeatherForecastService _service;

    public WeatherForecastServiceTests()
    {
        _repository = Substitute.For<IWeatherForecastRepository>();
        _unitOfWork = Substitute.For<IWeatherForecastUnitOfWork>();
        _service = new WeatherForecastService(
            _repository,
            _unitOfWork,
            NullLogger<WeatherForecastService>.Instance);
    }

    [Fact]
    public async Task GenerateForecastsAsync_WithValidParameters_ReturnsForecasts()
    {
        var results = await _service.GenerateForecastsAsync("New York", 5);

        Assert.Equal(5, results.Count);
        Assert.All(results, r =>
        {
            Assert.Equal("New York", r.Location);
            Assert.NotEqual(Guid.Empty, r.EntityId);
            Assert.False(string.IsNullOrEmpty(r.Summary));
        });
    }

    [Fact]
    public async Task GenerateForecastsAsync_ReturnsConsecutiveDates()
    {
        var results = await _service.GenerateForecastsAsync("London", 3);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        Assert.Equal(today, results[0].ForecastDate);
        Assert.Equal(today.AddDays(1), results[1].ForecastDate);
        Assert.Equal(today.AddDays(2), results[2].ForecastDate);
    }

    [Fact]
    public async Task GenerateForecastsAsync_AddsEntitiesToRepository()
    {
        await _service.GenerateForecastsAsync("Tokyo", 3);

        _repository.Received(3).Add(Arg.Any<WeatherForecastEntity>());
    }

    [Fact]
    public async Task GenerateForecastsAsync_CommitsUnitOfWork()
    {
        await _service.GenerateForecastsAsync("Paris", 5);

        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateForecastsAsync_WithCancellationToken_PassesToUnitOfWork()
    {
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        await _service.GenerateForecastsAsync("Berlin", 2, token);

        await _unitOfWork.Received(1).CommitAsync(token);
    }

    [Fact]
    public async Task GenerateForecastsAsync_WithNullLocation_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.GenerateForecastsAsync(null!, 5));
    }

    [Fact]
    public async Task GenerateForecastsAsync_WithZeroDays_ThrowsArgumentOutOfRangeException()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _service.GenerateForecastsAsync("NYC", 0));
    }

    [Fact]
    public async Task GenerateForecastsAsync_WithNegativeDays_ThrowsArgumentOutOfRangeException()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _service.GenerateForecastsAsync("NYC", -1));
    }

    [Fact]
    public async Task GenerateForecastsAsync_WithMoreThan14Days_ThrowsArgumentOutOfRangeException()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _service.GenerateForecastsAsync("NYC", 15));
    }

    [Fact]
    public async Task GenerateForecastsAsync_WhenCommitFails_PropagatesException()
    {
        _unitOfWork
            .CommitAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("Commit failed")));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.GenerateForecastsAsync("NYC", 3));
    }

    [Fact]
    public async Task GenerateForecastsAsync_HighTemperatureAlwaysGreaterOrEqualToLow()
    {
        var results = await _service.GenerateForecastsAsync("Moscow", 14);

        Assert.All(results, r =>
        {
            Assert.True(r.HighTemperatureCelsius >= r.LowTemperatureCelsius,
                $"High ({r.HighTemperatureCelsius}) should be >= Low ({r.LowTemperatureCelsius}) on {r.ForecastDate}");
        });
    }

    [Fact]
    public async Task GenerateForecastsAsync_FahrenheitConversionsAreCorrect()
    {
        var results = await _service.GenerateForecastsAsync("Sydney", 3);

        Assert.All(results, r =>
        {
            var expectedHighF = r.HighTemperatureCelsius * 9.0 / 5.0 + 32.0;
            var expectedLowF = r.LowTemperatureCelsius * 9.0 / 5.0 + 32.0;
            Assert.Equal(expectedHighF, r.HighTemperatureFahrenheit, 5);
            Assert.Equal(expectedLowF, r.LowTemperatureFahrenheit, 5);
        });
    }

    [Fact]
    public async Task GenerateForecastsAsync_WithSingleDay_ReturnsOneResult()
    {
        var results = await _service.GenerateForecastsAsync("Oslo", 1);

        Assert.Single(results);
    }
}

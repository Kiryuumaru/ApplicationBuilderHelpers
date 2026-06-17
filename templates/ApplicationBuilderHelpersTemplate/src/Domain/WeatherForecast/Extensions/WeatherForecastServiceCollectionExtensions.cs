using Microsoft.Extensions.DependencyInjection;

namespace Domain.WeatherForecast.Extensions;

internal static class WeatherForecastServiceCollectionExtensions
{
    internal static IServiceCollection AddWeatherForecastServices(this IServiceCollection services)
    {
        return services;
    }
}

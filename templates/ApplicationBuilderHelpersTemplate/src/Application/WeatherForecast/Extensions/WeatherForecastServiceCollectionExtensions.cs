using Application.Shared.Extensions;
using Application.WeatherForecast.EventHandlers;
using Application.WeatherForecast.Interfaces.Inbound;
using Application.WeatherForecast.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Application.WeatherForecast.Extensions;

internal static class WeatherForecastServiceCollectionExtensions
{
    internal static IServiceCollection AddWeatherForecastServices(this IServiceCollection services)
    {
        services.AddScoped<IWeatherForecastService, WeatherForecastService>();

        services.AddDomainEventHandler<LogForecastHandler>();
        services.AddDomainEventHandler<NotifySubscribersHandler>();

        return services;
    }
}

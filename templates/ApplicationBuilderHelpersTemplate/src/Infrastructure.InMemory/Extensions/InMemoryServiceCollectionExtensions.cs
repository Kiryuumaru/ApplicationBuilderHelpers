using Domain.WeatherForecast.Interfaces;
using Infrastructure.InMemory.Adapters;
using Infrastructure.InMemory.Interfaces;
using Infrastructure.InMemory.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.InMemory.Extensions;

internal static class InMemoryServiceCollectionExtensions
{
    internal static IServiceCollection AddInMemoryServices(this IServiceCollection services)
    {
        services.AddScoped<IWeatherForecastUnitOfWork, InMemoryWeatherForecastUnitOfWork>();

        services.AddScoped<InMemoryWeatherForecastRepository>();
        services.AddScoped<IWeatherForecastRepository>(sp => sp.GetRequiredService<InMemoryWeatherForecastRepository>());
        services.AddScoped<ITrackableRepository>(sp => sp.GetRequiredService<InMemoryWeatherForecastRepository>());

        return services;
    }
}

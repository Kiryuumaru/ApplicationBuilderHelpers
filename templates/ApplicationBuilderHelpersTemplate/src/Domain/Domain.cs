using Domain.AppEnvironment.Extensions;
using Domain.Shared.Extensions;
using Domain.WeatherForecast.Extensions;
using ApplicationBuilderHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace Domain;

public class Domain : ApplicationDependency
{
    public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
    {
        base.AddServices(applicationBuilder, services);

        services.AddSharedServices();
        services.AddAppEnvironmentServices();
        services.AddWeatherForecastServices();
    }
}

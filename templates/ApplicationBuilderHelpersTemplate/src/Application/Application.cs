using Application.AppEnvironment.Extensions;
using Application.EmbeddedConfig.Extensions;
using Application.Shared.Extensions;
using Application.WeatherForecast.Extensions;
using ApplicationBuilderHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public class Application : ApplicationDependency
{
    public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
    {
        base.AddServices(applicationBuilder, services);

        services.AddAppEnvironmentServices();
        services.AddEmbeddedConfigServices();
        services.AddSharedServices();
        services.AddWeatherForecastServices();
    }
}

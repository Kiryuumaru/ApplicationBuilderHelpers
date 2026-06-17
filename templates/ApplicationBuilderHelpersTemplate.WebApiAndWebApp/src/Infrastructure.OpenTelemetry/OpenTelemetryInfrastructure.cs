using ApplicationBuilderHelpers;
using Infrastructure.OpenTelemetry.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Infrastructure.OpenTelemetry;

public class OpenTelemetryInfrastructure : ApplicationDependency
{
    protected Dictionary<string, object?> LogDefaultScopeMap { get; set; } = [];

    public override void AddConfigurations(ApplicationHostBuilder applicationBuilder, IConfiguration configuration)
    {
        base.AddConfigurations(applicationBuilder, configuration);

        applicationBuilder.AddLoggerConfiguration(LogDefaultScopeMap);
    }

    public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
    {
        base.AddServices(applicationBuilder, services);

        applicationBuilder.AddLoggerServices();
    }

    public override void AddMiddlewares(ApplicationHost applicationHost, IHost host)
    {
        base.AddMiddlewares(applicationHost, host);

        applicationHost.AddLoggerMiddlewares();
    }

    public override void RunPreparation(ApplicationHost applicationHost)
    {
        base.RunPreparation(applicationHost);
    }
}

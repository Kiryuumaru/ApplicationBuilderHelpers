using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ApplicationBuilderHelpers.Test.Cli.Services;

namespace ApplicationBuilderHelpers.Test.Cli;

internal class Presentation : ApplicationDependency
{
    public override void AddConfigurations(ApplicationHostBuilder applicationBuilder, IConfiguration configuration)
    {
        base.AddConfigurations(applicationBuilder, configuration);

        (configuration as ConfigurationManager)!.AddEnvironmentVariables();
    }

    public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
    {
        base.AddServices(applicationBuilder, services);

        services.AddScoped<MockService>();
    }
}

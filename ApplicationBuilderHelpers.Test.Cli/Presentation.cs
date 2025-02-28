using ApplicationBuilderHelpers;
using Microsoft.Extensions.Options;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ApplicationBuilderHelpers.Test.Cli.Services;

namespace ApplicationBuilderHelpers.Test.Cli;

internal class Presentation : ApplicationDependency
{
    public override void AddConfiguration(ApplicationHostBuilder applicationBuilder, IConfiguration configuration)
    {
        base.AddConfiguration(applicationBuilder, configuration);

        (configuration as ConfigurationManager)!.AddEnvironmentVariables();
    }

    public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
    {
        base.AddServices(applicationBuilder, services);

        services.AddScoped<MockService>();

        Console.WriteLine("Hello from main PresentationPresentationPresentationPresentationPresentation");
    }
}

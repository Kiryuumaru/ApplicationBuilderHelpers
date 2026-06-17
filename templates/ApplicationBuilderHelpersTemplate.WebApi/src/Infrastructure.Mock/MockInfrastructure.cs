using ApplicationBuilderHelpers;
using Infrastructure.Mock.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Mock;

public sealed class MockInfrastructure : ApplicationDependency
{
    public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
    {
        base.AddServices(applicationBuilder, services);

        services.AddMockServices();
    }
}

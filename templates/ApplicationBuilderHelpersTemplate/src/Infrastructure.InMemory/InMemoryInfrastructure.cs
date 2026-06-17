using ApplicationBuilderHelpers;
using Infrastructure.InMemory.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.InMemory;

public class InMemoryInfrastructure : ApplicationDependency
{
    public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
    {
        base.AddServices(applicationBuilder, services);

        services.AddInMemoryServices();
    }
}

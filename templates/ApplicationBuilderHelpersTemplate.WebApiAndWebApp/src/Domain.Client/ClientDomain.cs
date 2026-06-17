using ApplicationBuilderHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace Domain.Client;

public class ClientDomain : ApplicationDependency
{
    public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
    {
        base.AddServices(applicationBuilder, services);
    }
}

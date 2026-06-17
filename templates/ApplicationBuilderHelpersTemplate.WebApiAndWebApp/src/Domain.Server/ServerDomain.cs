using ApplicationBuilderHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace Domain.Server;

public class ServerDomain : ApplicationDependency
{
    public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
    {
        base.AddServices(applicationBuilder, services);
    }
}

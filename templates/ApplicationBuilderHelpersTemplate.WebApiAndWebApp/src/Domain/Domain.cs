using ApplicationBuilderHelpers;
using Domain.Shared.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Domain;

public class Domain : ApplicationDependency
{
    public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
    {
        base.AddServices(applicationBuilder, services);

        services.AddSharedServices();
    }
}

using ApplicationBuilderHelpers;
using Infrastructure.EFCore.Server.Identity.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.EFCore.Server.Identity;

public class EFCoreServerIdentityInfrastructure : ApplicationDependency
{
    public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
    {
        base.AddServices(applicationBuilder, services);

        services.AddEFCoreIdentity();
    }
}

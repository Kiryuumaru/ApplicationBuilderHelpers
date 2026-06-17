using ApplicationBuilderHelpers;
using Infrastructure.EFCore.Identity.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.EFCore.Identity;

public sealed class EFCoreIdentityInfrastructure : ApplicationDependency
{
    public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
    {
        base.AddServices(applicationBuilder, services);

        services.AddEFCoreIdentity();
    }
}

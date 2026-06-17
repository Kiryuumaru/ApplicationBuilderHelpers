using Application.AppEnvironment.Extensions;
using Application.Shared.Extensions;
using Application.Credential.Extensions;
using Application.LocalStore.Extensions;
using Application.Logger.Extensions;
using ApplicationBuilderHelpers;
using Domain.AppEnvironment.Constants;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Application;

public class Application : ApplicationDependency
{
    public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
    {
        base.AddServices(applicationBuilder, services);

        services.AddSharedServices();
        services.AddAppEnvironmentServices();
        services.AddLocalStoreServices();
        services.AddCredentialServices();
    }

    public override void RunPreparation(ApplicationHost applicationHost)
    {
        base.RunPreparation(applicationHost);

#if DEBUG
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
        applicationHost.Configuration["ASPNETCORE_ENVIRONMENT"] = "Development";
#else
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");
        applicationHost.Configuration["ASPNETCORE_ENVIRONMENT"] = "Production";
#endif
    }
}

using Application.AppEnvironment.Extensions;
using Application.Authorization.Extensions;
using Application.Shared.Extensions;
using Application.EmbeddedConfig.Extensions;
using Application.Identity.Extensions;
using Application.LocalStore.Extensions;
using Application.Logger.Extensions;
using Application.NativeCmd.Extensions;
using ApplicationBuilderHelpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Application;

public sealed class Application : ApplicationDependency
{
    public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
    {
        base.AddServices(applicationBuilder, services);

        services.AddSharedServices();
        services.AddAppEnvironmentServices();
        services.AddAuthorizationServices();
        services.AddNativeCmdServices();
        services.AddLocalStoreServices();
        services.AddIdentityServices();
        services.AddEmbeddedConfigServices();
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

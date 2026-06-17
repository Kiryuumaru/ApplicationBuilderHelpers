using Application.Client.Authorization.Extensions;
using Application.Client.Identity.Extensions;
using Application.Client.Identity.Services;
using ApplicationBuilderHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Client;

public class ClientApplication : ApplicationDependency
{
    public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
    {
        base.AddServices(applicationBuilder, services);

        services.AddIdentityServices();
        services.AddAuthorizationServices();
    }

    public override async ValueTask RunPreparationAsync(ApplicationHost applicationHost, CancellationToken cancellationToken)
    {
        await base.RunPreparationAsync(applicationHost, cancellationToken);
        var authStateProvider = applicationHost.Services.GetRequiredService<ClientAuthStateProvider>();
        await authStateProvider.InitializeAsync();
    }
}

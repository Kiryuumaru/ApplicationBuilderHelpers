using ApplicationBuilderHelpers;
using Infrastructure.EFCore.Extensions;
using Infrastructure.EFCore.Interfaces;
using Infrastructure.EFCore.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.EFCore;

public class EFCoreInfrastructure : ApplicationDependency
{
    public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
    {
        base.AddServices(applicationBuilder, services);

        services.AddEFCoreInfrastructure();
    }

    public override async ValueTask RunPreparationAsync(ApplicationHost applicationHost, CancellationToken cancellationToken)
    {
        await base.RunPreparationAsync(applicationHost, cancellationToken);

        // Bootstrap database tables before the app starts
        var initializationState = applicationHost.Services.GetRequiredService<EFCoreDatabaseInitializationState>();
        if (!initializationState.IsInitialized)
        {
            using var scope = applicationHost.Services.CreateScope();
            var bootstrappers = scope.ServiceProvider.GetServices<IEFCoreDatabaseBootstrap>();
            foreach (var bootstrapper in bootstrappers)
            {
                await bootstrapper.SetupAsync(cancellationToken);
            }
            initializationState.MarkInitialized();
        }
    }
}

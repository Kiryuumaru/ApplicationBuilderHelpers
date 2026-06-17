using ApplicationBuilderHelpers;
using Infrastructure.EFCore.Sqlite.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.EFCore.Sqlite;

public sealed class EFCoreSqliteInfrastructure : ApplicationDependency
{
    public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
    {
        base.AddServices(applicationBuilder, services);

        services.AddEFCoreSqlite(applicationBuilder.Configuration);
    }
}

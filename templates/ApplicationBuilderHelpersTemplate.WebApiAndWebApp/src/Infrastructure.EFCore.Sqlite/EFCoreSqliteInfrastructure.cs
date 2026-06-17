using ApplicationBuilderHelpers;
using Infrastructure.EFCore.Sqlite.Extensions;
using Infrastructure.EFCore.Sqlite.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.EFCore.Sqlite;

public class EFCoreSqliteInfrastructure : ApplicationDependency
{
    public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
    {
        base.AddServices(applicationBuilder, services);

        services.AddEFCoreSqlite();
    }

    public override void RunPreparation(ApplicationHost applicationHost)
    {
        base.RunPreparation(applicationHost);

        // Ensure the keep-alive connection is open before any database operations.
        // This must happen before EFCore bootstrap runs EnsureCreatedAsync.
        var connectionHolder = applicationHost.Services.GetRequiredService<SqliteConnectionHolder>();
        connectionHolder.EnsureOpen();
    }
}

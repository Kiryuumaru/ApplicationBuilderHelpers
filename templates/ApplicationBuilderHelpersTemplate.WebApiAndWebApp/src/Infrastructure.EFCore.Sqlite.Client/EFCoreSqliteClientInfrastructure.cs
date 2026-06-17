using ApplicationBuilderHelpers;
using Infrastructure.EFCore.Sqlite.Extensions;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.EFCore.Sqlite.Client;

public class EFCoreSqliteClientInfrastructure : ApplicationDependency
{
    public override void AddConfigurations(ApplicationHostBuilder applicationBuilder, IConfiguration configuration)
    {
        base.AddConfigurations(applicationBuilder, configuration);

        configuration.SetSqliteConnectionString("Data Source=app.db");
    }
}

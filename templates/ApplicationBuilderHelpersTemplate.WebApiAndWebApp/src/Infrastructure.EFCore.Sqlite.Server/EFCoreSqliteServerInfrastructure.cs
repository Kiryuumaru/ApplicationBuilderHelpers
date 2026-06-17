using ApplicationBuilderHelpers;
using Infrastructure.EFCore.Sqlite.Extensions;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.EFCore.Sqlite.Server;

public class EFCoreSqliteServerInfrastructure : ApplicationDependency
{
    private const string SqliteConnectionStringKey = "SQLITE_CONNECTION_STRING";

    public override void AddConfigurations(ApplicationHostBuilder applicationBuilder, IConfiguration configuration)
    {
        base.AddConfigurations(applicationBuilder, configuration);

        try
        {
            _ = configuration.GetSqliteConnectionString();
        }
        catch
        {
            configuration.SetSqliteConnectionString("Data Source=app.db");
        }
    }
}

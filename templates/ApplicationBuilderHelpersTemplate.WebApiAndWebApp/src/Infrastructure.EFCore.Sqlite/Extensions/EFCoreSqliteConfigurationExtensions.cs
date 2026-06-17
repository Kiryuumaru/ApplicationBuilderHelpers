using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.EFCore.Sqlite.Extensions;

public static class EFCoreSqliteConfigurationExtensions
{
    private const string SqliteConnectionStringKey = "SQLITE_CONNECTION_STRING";

    public static string GetSqliteConnectionString(this IConfiguration configuration)
    {
        return configuration.GetRefValue(SqliteConnectionStringKey);
    }

    public static void SetSqliteConnectionString(this IConfiguration configuration, string connectionString)
    {
        configuration[SqliteConnectionStringKey] = connectionString;
    }
}

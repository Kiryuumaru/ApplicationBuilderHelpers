using Microsoft.Data.Sqlite;

namespace Infrastructure.EFCore.Sqlite.Services;

internal sealed class SqliteConnectionHolder : IDisposable
{
    private SqliteConnection? _keepAliveConnection;

    public SqliteConnectionHolder(string connectionString)
    {
        // For in-memory databases with shared cache, keep one connection open
        // to prevent the database from being destroyed when all connections close
        if (connectionString.Contains("Mode=Memory", StringComparison.OrdinalIgnoreCase))
        {
            _keepAliveConnection = new SqliteConnection(connectionString);
            _keepAliveConnection.Open();
        }
    }

    public void Dispose()
    {
        if (_keepAliveConnection != null)
        {
            _keepAliveConnection.Dispose();
            _keepAliveConnection = null;
        }
    }
}

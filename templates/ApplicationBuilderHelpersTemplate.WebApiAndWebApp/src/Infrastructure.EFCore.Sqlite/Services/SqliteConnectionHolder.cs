using Infrastructure.EFCore.Sqlite.Extensions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.EFCore.Sqlite.Services;

internal sealed class SqliteConnectionHolder : IDisposable
{
    private readonly string _connectionString;
    private SqliteConnection? _keepAliveConnection;

    public SqliteConnectionHolder(IConfiguration configuration)
    {
        _connectionString = configuration.GetSqliteConnectionString();
    }

    public SqliteConnectionHolder(string connectionString)
    {
        _connectionString = connectionString;
    }

    public void EnsureOpen()
    {
        if (_keepAliveConnection != null)
        {
            return;
        }

        // For in-memory databases with shared cache, keep one connection open
        // to prevent the database from being destroyed when all connections close
        if (_connectionString.Contains("Mode=Memory", StringComparison.OrdinalIgnoreCase))
        {
            _keepAliveConnection = new SqliteConnection(_connectionString);
            _keepAliveConnection.Open();
        }
    }

    public void Dispose()
    {
        _keepAliveConnection?.Dispose();
        _keepAliveConnection = null;
    }
}

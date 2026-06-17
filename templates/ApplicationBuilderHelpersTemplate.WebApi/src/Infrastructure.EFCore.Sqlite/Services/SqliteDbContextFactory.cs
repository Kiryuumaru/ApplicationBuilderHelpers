using Infrastructure.EFCore.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.EFCore.Sqlite.Services;

internal sealed class SqliteDbContextFactory(string connectionString, IEnumerable<IEFCoreEntityConfiguration> configurations) : IDbContextFactory<SqliteDbContext>
{
    public SqliteDbContext CreateDbContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<SqliteDbContext>();
        optionsBuilder.UseSqlite(connectionString);
        // Disable EF Core's internal service provider caching to ensure each factory
        // instance uses its own set of entity configurations. This prevents issues
        // where different configuration sets share a cached model.
        optionsBuilder.EnableServiceProviderCaching(false);
        return new SqliteDbContext(optionsBuilder.Options, configurations);
    }
}

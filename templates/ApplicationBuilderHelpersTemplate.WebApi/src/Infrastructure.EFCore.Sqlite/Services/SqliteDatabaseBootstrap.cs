using Infrastructure.EFCore.Interfaces;

namespace Infrastructure.EFCore.Sqlite.Services;

internal sealed class SqliteDatabaseBootstrap(SqliteDbContext dbContext) : IEFCoreDatabaseBootstrap
{
    public async Task SetupAsync(CancellationToken cancellationToken)
    {
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);
    }
}

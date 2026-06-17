using Infrastructure.EFCore.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.EFCore.Sqlite;

internal sealed class SqliteDbContext : EFCoreDbContext
{
    internal SqliteDbContext(DbContextOptions<SqliteDbContext> options, IEnumerable<IEFCoreEntityConfiguration> configurations) 
        : base(options, configurations)
    {
    }
}


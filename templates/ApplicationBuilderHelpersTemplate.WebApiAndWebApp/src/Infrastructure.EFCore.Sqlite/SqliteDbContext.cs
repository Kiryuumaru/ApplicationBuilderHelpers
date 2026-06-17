using System.Diagnostics.CodeAnalysis;
using Infrastructure.EFCore.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.EFCore.Sqlite;

// EF Core does not support NativeAOT without compiled models. No workaround currently exists.
// See: https://learn.microsoft.com/en-us/ef/core/performance/advanced-performance-topics#compiled-models
[UnconditionalSuppressMessage("AOT", "IL2026:RequiresUnreferencedCode", Justification = "EF Core inherently uses reflection. NativeAOT support requires compiled models.")]
public class SqliteDbContext(DbContextOptions<SqliteDbContext> options, IEnumerable<IEFCoreEntityConfiguration> configurations) : EFCoreDbContext(options, configurations)
{
}

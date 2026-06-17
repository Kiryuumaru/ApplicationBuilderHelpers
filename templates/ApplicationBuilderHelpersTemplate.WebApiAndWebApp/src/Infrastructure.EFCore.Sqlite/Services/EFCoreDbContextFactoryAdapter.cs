using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.EFCore.Sqlite.Services;

// EF Core does not support NativeAOT without compiled models. No workaround currently exists.
// See: https://learn.microsoft.com/en-us/ef/core/performance/advanced-performance-topics#compiled-models
[UnconditionalSuppressMessage("AOT", "IL2026:RequiresUnreferencedCode", Justification = "EF Core inherently uses reflection. NativeAOT support requires compiled models.")]
internal sealed class EFCoreDbContextFactoryAdapter(IDbContextFactory<SqliteDbContext> innerFactory) : IDbContextFactory<EFCoreDbContext>
{
    public EFCoreDbContext CreateDbContext() => innerFactory.CreateDbContext();
}

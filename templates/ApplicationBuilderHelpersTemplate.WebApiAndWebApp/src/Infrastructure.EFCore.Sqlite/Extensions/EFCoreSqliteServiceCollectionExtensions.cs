using System.Diagnostics.CodeAnalysis;
using ApplicationBuilderHelpers.Extensions;
using Infrastructure.EFCore.Interfaces;
using Infrastructure.EFCore.Sqlite.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.EFCore.Sqlite.Extensions;

// EF Core does not support NativeAOT without compiled models. No workaround currently exists.
// See: https://learn.microsoft.com/en-us/ef/core/performance/advanced-performance-topics#compiled-models
[UnconditionalSuppressMessage("AOT", "IL2026:RequiresUnreferencedCode", Justification = "EF Core inherently uses reflection. NativeAOT support requires compiled models.")]
internal static class EFCoreSqliteServiceCollectionExtensions
{
    public static IServiceCollection AddEFCoreSqlite(this IServiceCollection services)
    {
        // For in-memory databases with shared cache, keep one connection open
        // to prevent the database from being destroyed when all connections close
        services.AddSingleton<SqliteConnectionHolder>();

        // Register DbContext with factory pattern that injects entity configurations
        services.AddDbContext<SqliteDbContext>((sp, options) =>
        {
            options.UseSqlite(sp.GetRequiredService<IConfiguration>().GetSqliteConnectionString());
        });

        // Register factory that creates DbContext with configurations
        services.AddSingleton<IDbContextFactory<SqliteDbContext>>(sp =>
            new SqliteDbContextFactory(sp.GetRequiredService<IConfiguration>().GetSqliteConnectionString(), sp.GetServices<IEFCoreEntityConfiguration>()));

        // Register IDbContextFactory<EFCoreDbContext> for persistence-ignorant consumers
        services.AddSingleton<IDbContextFactory<EFCoreDbContext>>(sp =>
            new EFCoreDbContextFactoryAdapter(sp.GetRequiredService<IDbContextFactory<SqliteDbContext>>()));

        // Also register the base DbContext for scoped usage
        services.AddScoped<EFCoreDbContext>(sp => sp.GetRequiredService<SqliteDbContext>());

        services.AddScoped<IEFCoreDatabaseBootstrap, SqliteDatabaseBootstrap>();

        return services;
    }
}

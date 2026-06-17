using ApplicationBuilderHelpers.Extensions;
using Infrastructure.EFCore.Interfaces;
using Infrastructure.EFCore.Sqlite.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.EFCore.Sqlite.Extensions;

internal static class EFCoreSqliteServiceCollectionExtensions
{
    private const string DefaultConnectionString = "Data Source=app.db";

    public static IServiceCollection AddEFCoreSqlite(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetRefValueOrDefault("SQLITE_CONNECTION_STRING", DefaultConnectionString);
        return services.AddEFCoreSqlite(connectionString);
    }

    public static IServiceCollection AddEFCoreSqlite(this IServiceCollection services, string connectionString)
    {
        // For in-memory databases with shared cache, keep one connection open
        // to prevent the database from being destroyed when all connections close
        services.AddSingleton(new SqliteConnectionHolder(connectionString));
        
        // Register factory that creates DbContext with configurations
        services.AddSingleton<IDbContextFactory<SqliteDbContext>>(sp =>
        {
            var configurations = sp.GetServices<IEFCoreEntityConfiguration>();
            return new SqliteDbContextFactory(connectionString, configurations);
        });

        // Register scoped DbContext via factory (constructors are internal)
        services.AddScoped<SqliteDbContext>(sp =>
            sp.GetRequiredService<IDbContextFactory<SqliteDbContext>>().CreateDbContext());

        // Register IDbContextFactory<EFCoreDbContext> for persistence-ignorant consumers
        services.AddSingleton<IDbContextFactory<EFCoreDbContext>>(sp => 
            new EFCoreDbContextFactoryAdapter(sp.GetRequiredService<IDbContextFactory<SqliteDbContext>>()));

        // Also register the base DbContext for scoped usage
        services.AddScoped<EFCoreDbContext>(sp => sp.GetRequiredService<SqliteDbContext>());

        services.AddScoped<IEFCoreDatabaseBootstrap, SqliteDatabaseBootstrap>();

        return services;
    }
}

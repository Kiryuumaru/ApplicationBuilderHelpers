using Infrastructure.EFCore.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.EFCore;

internal class EFCoreDbContext : DbContext
{
    private readonly IEnumerable<IEFCoreEntityConfiguration> _configurations;

    internal EFCoreDbContext(DbContextOptions options, IEnumerable<IEFCoreEntityConfiguration> configurations)
        : base(options)
    {
        _configurations = configurations;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all registered entity configurations
        foreach (var configuration in _configurations)
        {
            configuration.Configure(modelBuilder);
        }
    }
}

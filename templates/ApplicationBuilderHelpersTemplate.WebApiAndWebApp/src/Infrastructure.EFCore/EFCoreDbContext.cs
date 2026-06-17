using System.Diagnostics.CodeAnalysis;
using Infrastructure.EFCore.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.EFCore;

[RequiresUnreferencedCode("EF Core uses reflection for model building. Use compiled models for full NativeAOT support.")]
public class EFCoreDbContext : DbContext
{
    private readonly IEnumerable<IEFCoreEntityConfiguration> _configurations;

    public EFCoreDbContext(DbContextOptions<EFCoreDbContext> options, IEnumerable<IEFCoreEntityConfiguration> configurations)
        : base(options)
    {
        _configurations = configurations;
    }

    protected EFCoreDbContext(DbContextOptions options, IEnumerable<IEFCoreEntityConfiguration> configurations)
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

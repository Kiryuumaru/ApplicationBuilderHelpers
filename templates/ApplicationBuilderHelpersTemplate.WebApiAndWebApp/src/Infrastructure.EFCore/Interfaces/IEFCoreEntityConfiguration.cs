using Microsoft.EntityFrameworkCore;

namespace Infrastructure.EFCore.Interfaces;

/// <summary>
/// Interface for modular EF Core entity configurations.
/// Each infrastructure module can register its own entity configurations.
/// </summary>
public interface IEFCoreEntityConfiguration
{
    /// <summary>
    /// Configures the entity model for this module.
    /// </summary>
    /// <param name="modelBuilder">The model builder to configure.</param>
    void Configure(ModelBuilder modelBuilder);
}

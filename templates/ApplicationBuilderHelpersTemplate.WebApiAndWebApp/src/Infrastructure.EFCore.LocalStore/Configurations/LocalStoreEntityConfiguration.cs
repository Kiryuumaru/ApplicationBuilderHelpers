using Infrastructure.EFCore.Interfaces;
using Infrastructure.EFCore.LocalStore.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.EFCore.LocalStore.Configurations;

internal class LocalStoreEntityConfiguration : IEFCoreEntityConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LocalStoreEntry>(entity =>
        {
            entity.ToTable("LocalStore");
            entity.HasKey(e => new { e.Group, e.Id });
            entity.Property(e => e.Group).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Id).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Data);
        });
    }
}

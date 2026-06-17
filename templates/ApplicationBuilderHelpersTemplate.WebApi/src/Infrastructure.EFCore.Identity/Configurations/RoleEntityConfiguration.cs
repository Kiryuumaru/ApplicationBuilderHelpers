using Infrastructure.EFCore.Identity.Models;
using Infrastructure.EFCore.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.EFCore.Identity.Configurations;

internal sealed class RoleEntityConfiguration : IEFCoreEntityConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<RoleEntity>();

        entity.ToTable("Roles");

        entity.HasKey(r => r.Id);

        entity.Property(r => r.Id)
            .HasConversion(id => id.ToString(), str => Guid.Parse(str))
            .IsRequired();

        entity.Property(r => r.RevId)
            .HasConversion(
                id => id.HasValue ? id.Value.ToString() : null,
                str => string.IsNullOrEmpty(str) ? null : Guid.Parse(str));

        entity.Property(r => r.Code).IsRequired().HasMaxLength(100);
        entity.Property(r => r.Name).IsRequired().HasMaxLength(256);
        entity.Property(r => r.NormalizedName).IsRequired().HasMaxLength(256);
        entity.Property(r => r.Description).HasMaxLength(1000);
        entity.Property(r => r.ScopeTemplatesJson).HasColumnName("ScopeTemplatesJson");

        entity.HasIndex(r => r.Code).IsUnique();
        entity.HasIndex(r => r.NormalizedName).IsUnique();
    }
}

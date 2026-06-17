using Infrastructure.EFCore.Identity.Models;
using Infrastructure.EFCore.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.EFCore.Identity.Configurations;

internal sealed class UserPermissionGrantEntityConfiguration : IEFCoreEntityConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<UserPermissionGrantEntity>();

        entity.ToTable("UserPermissionGrants");

        entity.HasKey(upg => new { upg.UserId, upg.PermissionIdentifier });

        entity.Property(upg => upg.UserId)
            .HasConversion(id => id.ToString(), str => Guid.Parse(str))
            .IsRequired();
        entity.Property(upg => upg.PermissionIdentifier).IsRequired().HasMaxLength(512);
        entity.Property(upg => upg.Description).HasMaxLength(1000);
        entity.Property(upg => upg.GrantedAt)
            .HasConversion(
                v => v.ToUnixTimeMilliseconds(),
                v => DateTimeOffset.FromUnixTimeMilliseconds(v))
            .IsRequired();
        entity.Property(upg => upg.GrantedBy).HasMaxLength(256);

        entity.HasIndex(upg => upg.UserId);
    }
}

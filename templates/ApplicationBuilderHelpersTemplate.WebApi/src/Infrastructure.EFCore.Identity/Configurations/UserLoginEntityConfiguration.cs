using Infrastructure.EFCore.Identity.Models;
using Infrastructure.EFCore.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.EFCore.Identity.Configurations;

internal sealed class UserLoginEntityConfiguration : IEFCoreEntityConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<UserLoginEntity>();

        entity.ToTable("UserLogins");

        entity.HasKey(ul => new { ul.LoginProvider, ul.ProviderKey });

        entity.Property(ul => ul.LoginProvider).IsRequired().HasMaxLength(128);
        entity.Property(ul => ul.ProviderKey).IsRequired().HasMaxLength(128);
        entity.Property(ul => ul.UserId)
            .HasConversion(id => id.ToString(), str => Guid.Parse(str))
            .IsRequired();
        entity.Property(ul => ul.ProviderDisplayName).HasMaxLength(256);
        entity.Property(ul => ul.Email).HasMaxLength(256);
        entity.Property(ul => ul.LinkedAt)
            .HasConversion(
                v => v.ToUnixTimeMilliseconds(),
                v => DateTimeOffset.FromUnixTimeMilliseconds(v));

        entity.HasIndex(ul => ul.UserId);
    }
}

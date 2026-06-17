using Infrastructure.EFCore.Identity.Models;
using Infrastructure.EFCore.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.EFCore.Identity.Configurations;

internal sealed class ApiKeyEntityConfiguration : IEFCoreEntityConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<ApiKeyEntity>();

        entity.ToTable("ApiKeys");

        entity.HasKey(k => k.Id);

        entity.Property(k => k.Id)
            .HasConversion(id => id.ToString(), str => Guid.Parse(str))
            .IsRequired();
        entity.Property(k => k.UserId)
            .HasConversion(id => id.ToString(), str => Guid.Parse(str))
            .IsRequired();
        entity.Property(k => k.Name).IsRequired().HasMaxLength(100);
        entity.Property(k => k.CreatedAt)
            .HasConversion(
                v => v.ToUnixTimeMilliseconds(),
                v => DateTimeOffset.FromUnixTimeMilliseconds(v))
            .IsRequired();
        entity.Property(k => k.ExpiresAt)
            .HasConversion(
                v => v.HasValue ? v.Value.ToUnixTimeMilliseconds() : (long?)null,
                v => v.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(v.Value) : null);
        entity.Property(k => k.LastUsedAt)
            .HasConversion(
                v => v.HasValue ? v.Value.ToUnixTimeMilliseconds() : (long?)null,
                v => v.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(v.Value) : null);
        entity.Property(k => k.IsRevoked).IsRequired();
        entity.Property(k => k.RevokedAt)
            .HasConversion(
                v => v.HasValue ? v.Value.ToUnixTimeMilliseconds() : (long?)null,
                v => v.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(v.Value) : null);

        entity.HasIndex(k => k.UserId);
        entity.HasIndex(k => new { k.UserId, k.IsRevoked });  // For active API keys query
        entity.HasIndex(k => k.ExpiresAt);  // For cleanup queries
    }
}

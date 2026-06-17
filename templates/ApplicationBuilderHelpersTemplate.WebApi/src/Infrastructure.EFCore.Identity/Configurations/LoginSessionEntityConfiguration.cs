using Infrastructure.EFCore.Identity.Models;
using Infrastructure.EFCore.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.EFCore.Identity.Configurations;

internal sealed class LoginSessionEntityConfiguration : IEFCoreEntityConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<LoginSessionEntity>();

        entity.ToTable("LoginSessions");

        entity.HasKey(s => s.Id);

        entity.Property(s => s.Id)
            .HasConversion(id => id.ToString(), str => Guid.Parse(str))
            .IsRequired();
        entity.Property(s => s.UserId)
            .HasConversion(id => id.ToString(), str => Guid.Parse(str))
            .IsRequired();
        entity.Property(s => s.RefreshTokenHash).IsRequired().HasMaxLength(256);
        entity.Property(s => s.DeviceName).HasMaxLength(256);
        entity.Property(s => s.UserAgent).HasMaxLength(512);
        entity.Property(s => s.IpAddress).HasMaxLength(64);
        entity.Property(s => s.CreatedAt)
            .HasConversion(
                v => v.ToUnixTimeMilliseconds(),
                v => DateTimeOffset.FromUnixTimeMilliseconds(v))
            .IsRequired();
        entity.Property(s => s.LastUsedAt)
            .HasConversion(
                v => v.ToUnixTimeMilliseconds(),
                v => DateTimeOffset.FromUnixTimeMilliseconds(v))
            .IsRequired();
        entity.Property(s => s.ExpiresAt)
            .HasConversion(
                v => v.ToUnixTimeMilliseconds(),
                v => DateTimeOffset.FromUnixTimeMilliseconds(v))
            .IsRequired();
        entity.Property(s => s.IsRevoked).IsRequired();
        entity.Property(s => s.RevokedAt)
            .HasConversion(
                v => v.HasValue ? v.Value.ToUnixTimeMilliseconds() : (long?)null,
                v => v.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(v.Value) : null);

        entity.HasIndex(s => s.UserId);
        entity.HasIndex(s => s.ExpiresAt);  // For cleanup queries
        entity.HasIndex(s => new { s.UserId, s.IsRevoked });  // For active sessions query
    }
}

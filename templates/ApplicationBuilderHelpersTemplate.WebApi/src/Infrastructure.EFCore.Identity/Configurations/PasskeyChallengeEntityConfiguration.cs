using Infrastructure.EFCore.Identity.Models;
using Infrastructure.EFCore.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.EFCore.Identity.Configurations;

internal sealed class PasskeyChallengeEntityConfiguration : IEFCoreEntityConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<PasskeyChallengeEntity>();

        entity.ToTable("PasskeyChallenges");

        entity.HasKey(c => c.Id);

        entity.Property(c => c.Id)
            .HasConversion(id => id.ToString(), str => Guid.Parse(str))
            .IsRequired();
        entity.Property(c => c.Challenge).IsRequired();
        entity.Property(c => c.UserId)
            .HasConversion(id => id.HasValue ? id.Value.ToString() : null, str => str != null ? Guid.Parse(str) : null);
        entity.Property(c => c.Type).IsRequired();
        entity.Property(c => c.OptionsJson).IsRequired();
        entity.Property(c => c.CredentialName).HasMaxLength(256);  // Optional, for registration
        entity.Property(c => c.CreatedAt)
            .HasConversion(
                v => v.ToUnixTimeMilliseconds(),
                v => DateTimeOffset.FromUnixTimeMilliseconds(v))
            .IsRequired();
        entity.Property(c => c.ExpiresAt)
            .HasConversion(
                v => v.ToUnixTimeMilliseconds(),
                v => DateTimeOffset.FromUnixTimeMilliseconds(v))
            .IsRequired();

        entity.HasIndex(c => c.ExpiresAt);  // For cleanup queries
    }
}

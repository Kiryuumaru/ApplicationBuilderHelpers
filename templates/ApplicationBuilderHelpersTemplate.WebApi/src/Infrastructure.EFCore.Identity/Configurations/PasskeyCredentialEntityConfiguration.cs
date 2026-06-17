using Infrastructure.EFCore.Identity.Models;
using Infrastructure.EFCore.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.EFCore.Identity.Configurations;

internal sealed class PasskeyCredentialEntityConfiguration : IEFCoreEntityConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<PasskeyCredentialEntity>();

        entity.ToTable("PasskeyCredentials");

        entity.HasKey(c => c.Id);

        entity.Property(c => c.Id)
            .HasConversion(id => id.ToString(), str => Guid.Parse(str))
            .IsRequired();
        entity.Property(c => c.UserId)
            .HasConversion(id => id.ToString(), str => Guid.Parse(str))
            .IsRequired();
        entity.Property(c => c.Name).IsRequired().HasMaxLength(256);
        entity.Property(c => c.CredentialId).IsRequired();
        entity.Property(c => c.PublicKey).IsRequired();
        entity.Property(c => c.SignCount).IsRequired();
        entity.Property(c => c.AaGuid)
            .HasConversion(id => id.ToString(), str => Guid.Parse(str))
            .IsRequired();
        entity.Property(c => c.CredentialType).IsRequired().HasMaxLength(50);
        entity.Property(c => c.RegisteredAt)
            .HasConversion(
                v => v.ToUnixTimeMilliseconds(),
                v => DateTimeOffset.FromUnixTimeMilliseconds(v))
            .IsRequired();
        entity.Property(c => c.LastUsedAt)
            .HasConversion(
                v => v.HasValue ? v.Value.ToUnixTimeMilliseconds() : (long?)null,
                v => v.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(v.Value) : null);
        entity.Property(c => c.UserHandle).IsRequired();
        entity.Property(c => c.AttestationFormat).IsRequired().HasMaxLength(50);
        entity.Property(c => c.CreatedAt)
            .HasConversion(
                v => v.HasValue ? v.Value.ToUnixTimeMilliseconds() : (long?)null,
                v => v.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(v.Value) : null);
        entity.Property(c => c.UpdatedAt)
            .HasConversion(
                v => v.HasValue ? v.Value.ToUnixTimeMilliseconds() : (long?)null,
                v => v.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(v.Value) : null);

        entity.HasIndex(c => c.UserId);
        entity.HasIndex(c => c.CredentialId);
        entity.HasIndex(c => c.UserHandle);
    }
}

using Infrastructure.EFCore.Identity.Models;
using Infrastructure.EFCore.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.EFCore.Identity.Configurations;

internal sealed class UserPasskeyEntityConfiguration : IEFCoreEntityConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<UserPasskeyEntity>();

        entity.ToTable("UserPasskeys");

        entity.HasKey(up => new { up.UserId, up.CredentialId });

        entity.Property(up => up.UserId)
            .HasConversion(id => id.ToString(), str => Guid.Parse(str))
            .IsRequired();
        entity.Property(up => up.CredentialId).IsRequired();
        entity.Property(up => up.PublicKey);
        entity.Property(up => up.Name).HasMaxLength(256);
        entity.Property(up => up.CreatedAt)
            .HasConversion(
                v => v.ToUnixTimeMilliseconds(),
                v => DateTimeOffset.FromUnixTimeMilliseconds(v));
        entity.Property(up => up.SignCount);
        entity.Property(up => up.Transports);
        entity.Property(up => up.AttestationObject);
        entity.Property(up => up.ClientDataJson);

        entity.HasIndex(up => up.CredentialId);
    }
}

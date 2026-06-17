using Domain.Identity.Entities;
using Infrastructure.EFCore.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.EFCore.Identity.Configurations;

internal sealed class UserEntityConfiguration : IEFCoreEntityConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<User>();

        entity.ToTable("Users");

        entity.HasKey(u => u.Id);

        entity.Property(u => u.Id)
            .HasConversion(id => id.ToString(), str => Guid.Parse(str))
            .IsRequired();

        entity.Property(u => u.RevId)
            .HasConversion(id => id.ToString(), str => Guid.Parse(str));

        entity.Property(u => u.UserName).HasMaxLength(256);
        entity.Property(u => u.NormalizedUserName).HasMaxLength(256);
        entity.Property(u => u.Email).HasMaxLength(256);
        entity.Property(u => u.NormalizedEmail).HasMaxLength(256);
        entity.Property(u => u.PasswordHash);
        entity.Property(u => u.SecurityStamp);
        entity.Property(u => u.PhoneNumber).HasMaxLength(20);
        entity.Property(u => u.AuthenticatorKey);
        entity.Property(u => u.RecoveryCodes);

        entity.HasIndex(u => u.NormalizedUserName).IsUnique();
        entity.HasIndex(u => u.NormalizedEmail);

        // Map login tracking fields for cleanup queries - store as Unix milliseconds for LINQ translation
        entity.Property(u => u.LastLoginAt)
            .HasConversion(
                v => v.HasValue ? v.Value.ToUnixTimeMilliseconds() : (long?)null,
                v => v.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(v.Value) : null);
        entity.Property(u => u.LastFailedLoginAt)
            .HasConversion(
                v => v.HasValue ? v.Value.ToUnixTimeMilliseconds() : (long?)null,
                v => v.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(v.Value) : null);
        
        // Map anonymous user fields for cleanup queries
        entity.Property(u => u.IsAnonymous);
        entity.Property(u => u.LinkedAt)
            .HasConversion(
                v => v.HasValue ? v.Value.ToUnixTimeMilliseconds() : (long?)null,
                v => v.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(v.Value) : null);
        
        // Map lockout field
        entity.Property(u => u.LockoutEnd)
            .HasConversion(
                v => v.HasValue ? v.Value.ToUnixTimeMilliseconds() : (long?)null,
                v => v.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(v.Value) : null);
        
        // Map audit fields from AuditableEntity base class
        entity.Property(u => u.Created)
            .HasConversion(
                v => v.ToUnixTimeMilliseconds(),
                v => DateTimeOffset.FromUnixTimeMilliseconds(v));
        entity.Property(u => u.LastModified)
            .HasConversion(
                v => v.ToUnixTimeMilliseconds(),
                v => DateTimeOffset.FromUnixTimeMilliseconds(v));
        
        // Ignore navigation properties - we'll store them in separate tables
        entity.Ignore(u => u.PermissionGrants);
        entity.Ignore(u => u.RoleIds);
        entity.Ignore(u => u.RoleAssignments);
        entity.Ignore(u => u.IdentityLinks);
        entity.Ignore(u => u.Status);
    }
}

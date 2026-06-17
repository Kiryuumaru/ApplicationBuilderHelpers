using Domain.Authorization.Entities;
using Domain.Authorization.Models;
using Domain.Identity.Entities;
using Domain.Identity.Models;
using Infrastructure.EFCore.Server.Identity.Models;
using Infrastructure.EFCore.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.EFCore.Server.Identity.Configurations;

internal class IdentityEntityConfiguration : IEFCoreEntityConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        ConfigureUser(modelBuilder);
        ConfigureRole(modelBuilder);
        ConfigureUserLogin(modelBuilder);
        ConfigureUserPasskey(modelBuilder);  // For SignInManager/Blazor cookie-based passkeys
        ConfigurePasskeyChallenge(modelBuilder);  // For REST API passkeys
        ConfigurePasskeyCredential(modelBuilder);  // For REST API passkeys
        ConfigureUserRoleAssignment(modelBuilder);
        ConfigureUserPermissionGrant(modelBuilder);
        ConfigureLoginSession(modelBuilder);
        ConfigureApiKey(modelBuilder);
    }

    private static void ConfigureUser(ModelBuilder modelBuilder)
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

    private static void ConfigureRole(ModelBuilder modelBuilder)
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

    private static void ConfigureUserLogin(ModelBuilder modelBuilder)
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

    private static void ConfigureUserPasskey(ModelBuilder modelBuilder)
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

    private static void ConfigurePasskeyChallenge(ModelBuilder modelBuilder)
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

    private static void ConfigurePasskeyCredential(ModelBuilder modelBuilder)
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

    private static void ConfigureUserRoleAssignment(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<UserRoleAssignmentEntity>();

        entity.ToTable("UserRoleAssignments");

        entity.HasKey(ura => new { ura.UserId, ura.RoleId });

        entity.Property(ura => ura.UserId)
            .HasConversion(id => id.ToString(), str => Guid.Parse(str))
            .IsRequired();
        entity.Property(ura => ura.RoleId)
            .HasConversion(id => id.ToString(), str => Guid.Parse(str))
            .IsRequired();
        entity.Property(ura => ura.ParameterValuesJson).HasMaxLength(4000);
        entity.Property(ura => ura.AssignedAt)
            .HasConversion(
                v => v.ToUnixTimeMilliseconds(),
                v => DateTimeOffset.FromUnixTimeMilliseconds(v));

        entity.HasIndex(ura => ura.UserId);
        entity.HasIndex(ura => ura.RoleId);
    }

    private static void ConfigureUserPermissionGrant(ModelBuilder modelBuilder)
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

    private static void ConfigureLoginSession(ModelBuilder modelBuilder)
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

    private static void ConfigureApiKey(ModelBuilder modelBuilder)
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

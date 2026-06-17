using Infrastructure.EFCore.Identity.Models;
using Infrastructure.EFCore.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.EFCore.Identity.Configurations;

internal sealed class UserRoleAssignmentEntityConfiguration : IEFCoreEntityConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
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
}

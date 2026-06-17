using Domain.Authorization.Entities;
using Infrastructure.EFCore.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.EFCore.Identity.Services;

internal sealed class EFCoreAspNetRoleStore(IDbContextFactory<EFCoreDbContext> contextFactory) :
    IRoleStore<Role>
{
    public void Dispose()
    {
        // No resources to dispose - DbContext is created per-operation
    }

    public async Task<IdentityResult> CreateAsync(Role role, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        context.Set<Role>().Add(role);
        await context.SaveChangesWithExceptionHandlingAsync("Role", role.Code, cancellationToken);
        return IdentityResult.Success;
    }

    public async Task<IdentityResult> DeleteAsync(Role role, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var existing = await context.Set<Role>().FindAsync([role.Id], cancellationToken);
        if (existing is not null)
        {
            context.Set<Role>().Remove(existing);
            await context.SaveChangesWithExceptionHandlingAsync(cancellationToken);
        }
        return IdentityResult.Success;
    }

    public async Task<Role?> FindByIdAsync(string roleId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(roleId, out var id))
        {
            return null;
        }

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Set<Role>().FindAsync([id], cancellationToken);
    }

    public async Task<Role?> FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Set<Role>()
            .FirstOrDefaultAsync(r => r.NormalizedName == normalizedRoleName, cancellationToken);
    }

    public Task<string?> GetNormalizedRoleNameAsync(Role role, CancellationToken cancellationToken)
        => Task.FromResult<string?>(role.NormalizedName);

    public Task<string> GetRoleIdAsync(Role role, CancellationToken cancellationToken)
        => Task.FromResult(role.Id.ToString());

    public Task<string?> GetRoleNameAsync(Role role, CancellationToken cancellationToken)
        => Task.FromResult<string?>(role.Name);

    public Task SetNormalizedRoleNameAsync(Role role, string? normalizedName, CancellationToken cancellationToken)
    {
        // Role entity handles this internally
        return Task.CompletedTask;
    }

    public Task SetRoleNameAsync(Role role, string? roleName, CancellationToken cancellationToken)
    {
        // Role.Code is set via factory method, not mutable
        return Task.CompletedTask;
    }

    public async Task<IdentityResult> UpdateAsync(Role role, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        context.Set<Role>().Update(role);
        await context.SaveChangesWithExceptionHandlingAsync("Role", role.Code, cancellationToken);
        return IdentityResult.Success;
    }
}

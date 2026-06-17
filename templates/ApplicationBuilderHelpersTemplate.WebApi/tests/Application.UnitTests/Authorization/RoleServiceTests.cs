using Application.Authorization.Models;
using Application.Authorization.Services;
using Application.UnitTests.Authorization.Fakes;
using Domain.Authorization.Constants;
using Domain.Authorization.ValueObjects;
using Domain.Shared.Exceptions;
using RolesConstants = Domain.Authorization.Constants.Roles;

namespace Application.UnitTests.Authorization;

public sealed class RoleServiceTests
{
    [Fact]
    public async Task ListAsync_IncludesStaticRolesWithoutDynamicData()
    {
        var repository = new InMemoryRoleRepository();
        var unitOfWork = new InMemoryAuthorizationUnitOfWork();
        var service = new RoleService(repository, unitOfWork);

        var roles = await service.ListAsync(CancellationToken.None);

        Assert.Contains(roles, role => role.Code == RolesConstants.Admin.Code);
        Assert.Contains(roles, role => role.Code == RolesConstants.User.Code);
    }

    [Fact]
    public async Task CreateRoleAsync_ThrowsForReservedSystemCode()
    {
        var repository = new InMemoryRoleRepository();
        var unitOfWork = new InMemoryAuthorizationUnitOfWork();
        var service = new RoleService(repository, unitOfWork);

        var descriptor = new RoleDescriptor(
            Code: RolesConstants.Admin.Code,
            Name: "Duplicate Admin",
            Description: null,
            IsSystemRole: false,
            ScopeTemplates: [ScopeTemplate.Allow(Permissions.RootReadIdentifier)]);

        await Assert.ThrowsAsync<ReservedNameException>(() => service.CreateRoleAsync(descriptor, CancellationToken.None));
    }

    [Fact]
    public async Task CreateRoleAsync_PreventsDuplicateCodes()
    {
        var repository = new InMemoryRoleRepository();
        var unitOfWork = new InMemoryAuthorizationUnitOfWork();
        var service = new RoleService(repository, unitOfWork);

        var descriptor = new RoleDescriptor(
            Code: "portfolio_manager",
            Name: "Portfolio Manager",
            Description: "Manages portfolios",
            IsSystemRole: false,
            ScopeTemplates:
            [
                ScopeTemplate.Allow("api:portfolio:accounts:list", ("userId", "user-123"))
            ]);

        await service.CreateRoleAsync(descriptor, CancellationToken.None);

        await Assert.ThrowsAsync<DuplicateEntityException>(() => service.CreateRoleAsync(descriptor, CancellationToken.None));
    }

    [Fact]
    public async Task ReplaceScopeTemplatesAsync_UpdatesRoleScopeTemplates()
    {
        var repository = new InMemoryRoleRepository();
        var unitOfWork = new InMemoryAuthorizationUnitOfWork();
        var service = new RoleService(repository, unitOfWork);

        var descriptor = new RoleDescriptor(
            Code: "analyst",
            Name: "Read Only Analyst",
            Description: null,
            IsSystemRole: false,
            ScopeTemplates:
            [
                ScopeTemplate.Allow("api:favorites:read")
            ]);

        var role = await service.CreateRoleAsync(descriptor, CancellationToken.None);

        role = await service.ReplaceScopeTemplatesAsync(
            role.Id,
            [ScopeTemplate.Allow(Permissions.RootReadIdentifier)],
            CancellationToken.None);

        Assert.Contains(role.ScopeTemplates, t => t.PermissionPath == Permissions.RootReadIdentifier);
        Assert.Single(role.ScopeTemplates);
    }
}

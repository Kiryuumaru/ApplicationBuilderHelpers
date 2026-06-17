using System.Text.Json;
using Domain.Identity.Enums;
using Domain.Identity.Interfaces;
using Domain.Identity.Models;
using Domain.Identity.Entities;
using Domain.Identity.ValueObjects;
using Infrastructure.EFCore.Extensions;
using Infrastructure.EFCore.Identity.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.EFCore.Identity.Repositories;

internal sealed class EFCoreUserRepository(EFCoreDbContext context) : IUserRepository
{
    // Query methods

    public async Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await context.Set<User>().FindAsync([id], cancellationToken);
        if (user is not null)
        {
            await HydrateRoleAssignmentsAsync(user, cancellationToken);
            await HydratePermissionGrantsAsync(user, cancellationToken);
        }
        return user;
    }

    public async Task<User?> FindByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        var normalizedUsername = username.ToUpperInvariant();
        var user = await context.Set<User>()
            .FirstOrDefaultAsync(u => u.NormalizedUserName == normalizedUsername, cancellationToken);
        if (user is not null)
        {
            await HydrateRoleAssignmentsAsync(user, cancellationToken);
            await HydratePermissionGrantsAsync(user, cancellationToken);
        }
        return user;
    }

    public async Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.ToUpperInvariant();
        var user = await context.Set<User>()
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);
        if (user is not null)
        {
            await HydrateRoleAssignmentsAsync(user, cancellationToken);
            await HydratePermissionGrantsAsync(user, cancellationToken);
        }
        return user;
    }

    public async Task<User?> FindByExternalIdentityAsync(string provider, string providerSubject, CancellationToken cancellationToken)
    {
        var login = await context.Set<UserLoginEntity>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                l => l.LoginProvider == provider && l.ProviderKey == providerSubject,
                cancellationToken);

        if (login is null)
        {
            return null;
        }

        var user = await context.Set<User>().FindAsync([login.UserId], cancellationToken);
        if (user is not null)
        {
            await HydrateRoleAssignmentsAsync(user, cancellationToken);
            await HydratePermissionGrantsAsync(user, cancellationToken);
        }
        return user;
    }

    public async Task<IReadOnlyCollection<User>> ListAsync(CancellationToken cancellationToken)
    {
        var users = await context.Set<User>().ToListAsync(cancellationToken);
        
        var userIds = users.Select(u => u.Id).ToList();
        var allAssignments = await context.Set<UserRoleAssignmentEntity>()
            .Where(a => userIds.Contains(a.UserId))
            .ToListAsync(cancellationToken);
        
        var assignmentsByUser = allAssignments
            .GroupBy(a => a.UserId)
            .ToDictionary(g => g.Key, g => g.ToList());
        
        foreach (var user in users)
        {
            if (assignmentsByUser.TryGetValue(user.Id, out var assignments))
            {
                foreach (var assignment in assignments)
                {
                    var parameterValues = DeserializeParameterValues(assignment.ParameterValuesJson);
                    user.AssignRole(assignment.RoleId, parameterValues);
                }
            }
        }
        
        return users.AsReadOnly();
    }

    public async Task<Guid?> FindUserByLoginAsync(
        ExternalLoginProvider provider,
        string providerKey,
        CancellationToken cancellationToken)
    {
        var providerName = provider.ToString();
        var login = await context.Set<UserLoginEntity>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                l => l.LoginProvider == providerName && l.ProviderKey == providerKey,
                cancellationToken);

        return login?.UserId;
    }

    public async Task<IReadOnlyCollection<ExternalLoginInfo>> GetLoginsAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var logins = await context.Set<UserLoginEntity>()
            .AsNoTracking()
            .Where(l => l.UserId == userId)
            .ToListAsync(cancellationToken);

        return logins.Select(l => new ExternalLoginInfo
        {
            Provider = l.LoginProvider,
            ProviderSubject = l.ProviderKey,
            DisplayName = l.ProviderDisplayName,
            Email = l.Email,
            LinkedAt = l.LinkedAt
        }).ToList();
    }

    public async Task<bool> HasAnyLoginAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await context.Set<UserLoginEntity>()
            .AsNoTracking()
            .AnyAsync(l => l.UserId == userId, cancellationToken);
    }

    // Change tracking methods - changes are persisted on UnitOfWork.CommitAsync()

    public void Add(User user)
    {
        context.Set<User>().Add(user);
        SyncRoleAssignments(user);
        SyncPermissionGrants(user);
    }

    public void Update(User user)
    {
        var entry = context.Entry(user);
        if (entry.State == EntityState.Detached)
        {
            context.Set<User>().Attach(user);
            entry.State = EntityState.Modified;
        }
        SyncRoleAssignments(user);
        SyncPermissionGrants(user);
    }

    public void Remove(User user)
    {
        context.Set<User>().Remove(user);
    }

    public void AddLogin(Guid userId, ExternalLoginProvider provider, string providerKey, string? displayName, string? email)
    {
        var providerName = provider.ToString();
        
        var existing = context.Set<UserLoginEntity>()
            .Local
            .FirstOrDefault(l => l.UserId == userId && l.LoginProvider == providerName);

        if (existing is not null)
        {
            existing.ProviderKey = providerKey;
            existing.ProviderDisplayName = displayName;
            existing.Email = email;
        }
        else
        {
            context.Set<UserLoginEntity>().Add(new UserLoginEntity
            {
                UserId = userId,
                LoginProvider = providerName,
                ProviderKey = providerKey,
                ProviderDisplayName = displayName,
                Email = email,
                LinkedAt = DateTimeOffset.UtcNow
            });
        }
    }

    public void RemoveLogin(Guid userId, ExternalLoginProvider provider)
    {
        var providerName = provider.ToString();
        var login = context.Set<UserLoginEntity>()
            .Local
            .FirstOrDefault(l => l.UserId == userId && l.LoginProvider == providerName);

        if (login is not null)
        {
            context.Set<UserLoginEntity>().Remove(login);
        }
    }

    // Bulk operation - executes immediately for efficiency (background cleanup)
    public async Task<int> DeleteAbandonedAnonymousUsersAsync(DateTimeOffset cutoffDate, CancellationToken cancellationToken)
    {
        var abandonedUsers = await context.Set<User>()
            .Where(u => u.IsAnonymous == true && u.LastLoginAt != null && u.LastLoginAt < cutoffDate)
            .ToListAsync(cancellationToken);

        context.Set<User>().RemoveRange(abandonedUsers);
        await context.SaveChangesWithExceptionHandlingAsync(cancellationToken);

        return abandonedUsers.Count;
    }

    // Helper methods

    private async Task HydrateRoleAssignmentsAsync(User user, CancellationToken cancellationToken)
    {
        var assignments = await context.Set<UserRoleAssignmentEntity>()
            .Where(a => a.UserId == user.Id)
            .ToListAsync(cancellationToken);

        foreach (var assignment in assignments)
        {
            var parameterValues = DeserializeParameterValues(assignment.ParameterValuesJson);
            user.AssignRole(assignment.RoleId, parameterValues);
        }
    }

    private async Task HydratePermissionGrantsAsync(User user, CancellationToken cancellationToken)
    {
        var grants = await context.Set<UserPermissionGrantEntity>()
            .Where(g => g.UserId == user.Id)
            .ToListAsync(cancellationToken);

        foreach (var grant in grants)
        {
            var permissionGrant = grant.Type == Domain.Authorization.Enums.ScopeDirectiveType.Allow
                ? UserPermissionGrant.Allow(
                    grant.PermissionIdentifier,
                    grant.Description,
                    grant.GrantedBy,
                    grant.GrantedAt)
                : UserPermissionGrant.Deny(
                    grant.PermissionIdentifier,
                    grant.Description,
                    grant.GrantedBy,
                    grant.GrantedAt);
            user.GrantPermission(permissionGrant);
        }
    }

    private void SyncRoleAssignments(User user)
    {
        var existingAssignments = context.Set<UserRoleAssignmentEntity>()
            .Local
            .Where(a => a.UserId == user.Id)
            .ToList();

        var currentAssignments = user.RoleAssignments;

        var toRemove = existingAssignments
            .Where(existing => !currentAssignments.Any(current => current.RoleId == existing.RoleId))
            .ToList();

        var existingRoleIds = existingAssignments.Select(a => a.RoleId).ToHashSet();
        var toAdd = currentAssignments
            .Where(current => !existingRoleIds.Contains(current.RoleId))
            .Select(assignment => new UserRoleAssignmentEntity
            {
                UserId = user.Id,
                RoleId = assignment.RoleId,
                ParameterValuesJson = SerializeParameterValues(assignment.ParameterValues),
                AssignedAt = DateTimeOffset.UtcNow
            })
            .ToList();

        foreach (var update in existingAssignments.Where(existing => 
            currentAssignments.Any(current => 
                current.RoleId == existing.RoleId && 
                SerializeParameterValues(current.ParameterValues) != existing.ParameterValuesJson)))
        {
            var current = currentAssignments.First(c => c.RoleId == update.RoleId);
            update.ParameterValuesJson = SerializeParameterValues(current.ParameterValues);
        }

        context.Set<UserRoleAssignmentEntity>().RemoveRange(toRemove);
        context.Set<UserRoleAssignmentEntity>().AddRange(toAdd);
    }

    private void SyncPermissionGrants(User user)
    {
        var existingGrants = context.Set<UserPermissionGrantEntity>()
            .Local
            .Where(g => g.UserId == user.Id)
            .ToList();

        var currentGrants = user.PermissionGrants;

        var toRemove = existingGrants
            .Where(existing => !currentGrants.Any(current => 
                string.Equals(current.Identifier, existing.PermissionIdentifier, StringComparison.Ordinal) &&
                current.Type == existing.Type))
            .ToList();

        var existingKeys = existingGrants
            .Select(g => (g.PermissionIdentifier, g.Type))
            .ToHashSet();
        var toAdd = currentGrants
            .Where(current => !existingKeys.Contains((current.Identifier, current.Type)))
            .Select(grant => new UserPermissionGrantEntity
            {
                UserId = user.Id,
                PermissionIdentifier = grant.Identifier,
                Type = grant.Type,
                Description = grant.Description,
                GrantedAt = grant.GrantedAt,
                GrantedBy = grant.GrantedBy
            })
            .ToList();

        context.Set<UserPermissionGrantEntity>().RemoveRange(toRemove);
        context.Set<UserPermissionGrantEntity>().AddRange(toAdd);
    }

    private static IReadOnlyDictionary<string, string?>? DeserializeParameterValues(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string?>>(json);
        }
        catch
        {
            return null;
        }
    }

    private static string? SerializeParameterValues(IReadOnlyDictionary<string, string?>? parameterValues)
    {
        if (parameterValues is null || parameterValues.Count == 0)
        {
            return null;
        }

        return JsonSerializer.Serialize(parameterValues);
    }
}

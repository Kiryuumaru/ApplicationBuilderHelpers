using System.Text.Json;
using Application.Server.Identity.Interfaces.Outbound;
using Application.Server.Identity.Models;
using Domain.Identity.Enums;
using Domain.Identity.Entities;
using Domain.Identity.Models;
using Domain.Identity.ValueObjects;
using Domain.Shared.Exceptions;
using Infrastructure.EFCore.Extensions;
using Infrastructure.EFCore.Server.Identity.Serialization;
using Infrastructure.EFCore.Server.Identity.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.EFCore.Server.Identity.Repositories;

internal sealed class EFCoreUserRepository(IDbContextFactory<EFCoreDbContext> contextFactory) : IUserRepository
{
    // User operations (from IUserStore)

    public async Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var user = await context.Set<User>().FindAsync([id], cancellationToken);
        if (user is not null)
        {
            await HydrateRoleAssignmentsAsync(context, user, cancellationToken);
            await HydratePermissionGrantsAsync(context, user, cancellationToken);
        }
        return user;
    }

    public async Task<User?> FindByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var normalizedUsername = username.ToUpperInvariant();
        var user = await context.Set<User>()
            .FirstOrDefaultAsync(u => u.NormalizedUserName == normalizedUsername, cancellationToken);
        if (user is not null)
        {
            await HydrateRoleAssignmentsAsync(context, user, cancellationToken);
            await HydratePermissionGrantsAsync(context, user, cancellationToken);
        }
        return user;
    }

    public async Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var normalizedEmail = email.ToUpperInvariant();
        var user = await context.Set<User>()
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);
        if (user is not null)
        {
            await HydrateRoleAssignmentsAsync(context, user, cancellationToken);
            await HydratePermissionGrantsAsync(context, user, cancellationToken);
        }
        return user;
    }

    public async Task<User?> FindByExternalIdentityAsync(string provider, string providerSubject, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        
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
            await HydrateRoleAssignmentsAsync(context, user, cancellationToken);
            await HydratePermissionGrantsAsync(context, user, cancellationToken);
        }
        return user;
    }

    public async Task SaveAsync(User user, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        
        var existing = await context.Set<User>().FindAsync([user.Id], cancellationToken);
        if (existing is null)
        {
            context.Set<User>().Add(user);
        }
        else
        {
            context.Entry(existing).CurrentValues.SetValues(user);
        }

        // Sync role assignments
        await SyncRoleAssignmentsAsync(context, user, cancellationToken);

        // Sync permission grants
        await SyncPermissionGrantsAsync(context, user, cancellationToken);

        await context.SaveChangesWithExceptionHandlingAsync(
            ex => MapUserConstraintViolation(ex, user),
            cancellationToken);
    }

    private static DuplicateEntityException MapUserConstraintViolation(DbUpdateException ex, User user)
    {
        var message = ex.InnerException?.Message ?? ex.Message;
        if (message.Contains("NormalizedUserName", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("UserName", StringComparison.OrdinalIgnoreCase))
        {
            return new DuplicateEntityException("Username", user.UserName ?? "unknown");
        }
        if (message.Contains("NormalizedEmail", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("Email", StringComparison.OrdinalIgnoreCase))
        {
            return new DuplicateEntityException("Email", user.Email ?? "unknown");
        }
        return new DuplicateEntityException("User", user.Id.ToString());
    }

    public async Task DeleteAsync(Guid userId, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        
        var user = await context.Set<User>().FindAsync([userId], cancellationToken);
        if (user is not null)
        {
            context.Set<User>().Remove(user);
            await context.SaveChangesWithExceptionHandlingAsync(cancellationToken);
        }
    }

    public async Task<IReadOnlyCollection<User>> ListAsync(CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var users = await context.Set<User>().ToListAsync(cancellationToken);
        
        // Load all role assignments for all users in a single query
        var userIds = users.Select(u => u.Id).ToList();
        var allAssignments = await context.Set<UserRoleAssignmentEntity>()
            .AsNoTracking()
            .Where(a => userIds.Contains(a.UserId))
            .ToListAsync(cancellationToken);
        
        // Group assignments by user ID for efficient lookup
        var assignmentsByUser = allAssignments
            .GroupBy(a => a.UserId)
            .ToDictionary(g => g.Key, g => g.ToList());
        
        // Hydrate role assignments for each user
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

    public async Task<int> DeleteAbandonedAnonymousUsersAsync(DateTimeOffset cutoffDate, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        
        var abandonedUsers = await context.Set<User>()
            .Where(u => u.IsAnonymous == true && u.LastLoginAt != null && u.LastLoginAt < cutoffDate)
            .ToListAsync(cancellationToken);

        context.Set<User>().RemoveRange(abandonedUsers);
        await context.SaveChangesWithExceptionHandlingAsync(cancellationToken);

        return abandonedUsers.Count;
    }

    // External login operations (from IExternalLoginStore)

    public async Task<Guid?> FindUserByLoginAsync(
        ExternalLoginProvider provider,
        string providerKey,
        CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var providerName = provider.ToString();
        var login = await context.Set<UserLoginEntity>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                l => l.LoginProvider == providerName && l.ProviderKey == providerKey,
                cancellationToken);

        return login?.UserId;
    }

    public async Task AddLoginAsync(
        Guid userId,
        ExternalLoginProvider provider,
        string providerKey,
        string? displayName,
        string? email,
        CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var providerName = provider.ToString();

        var existing = await context.Set<UserLoginEntity>()
            .FirstOrDefaultAsync(
                l => l.UserId == userId && l.LoginProvider == providerName,
                cancellationToken);

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

        await context.SaveChangesWithExceptionHandlingAsync(cancellationToken);
    }

    public async Task<bool> RemoveLoginAsync(
        Guid userId,
        ExternalLoginProvider provider,
        CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var providerName = provider.ToString();
        var login = await context.Set<UserLoginEntity>()
            .FirstOrDefaultAsync(
                l => l.UserId == userId && l.LoginProvider == providerName,
                cancellationToken);

        if (login is null)
        {
            return false;
        }

        context.Set<UserLoginEntity>().Remove(login);
        await context.SaveChangesWithExceptionHandlingAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyCollection<ExternalLoginInfo>> GetLoginsAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

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
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Set<UserLoginEntity>()
            .AsNoTracking()
            .AnyAsync(l => l.UserId == userId, cancellationToken);
    }

    // Helper methods for role assignment persistence

    private static async Task HydrateRoleAssignmentsAsync(EFCoreDbContext context, User user, CancellationToken cancellationToken)
    {
        var assignments = await context.Set<UserRoleAssignmentEntity>()
            .AsNoTracking()
            .Where(a => a.UserId == user.Id)
            .ToListAsync(cancellationToken);

        foreach (var assignment in assignments)
        {
            var parameterValues = DeserializeParameterValues(assignment.ParameterValuesJson);
            user.AssignRole(assignment.RoleId, parameterValues);
        }
    }

    private static async Task HydratePermissionGrantsAsync(EFCoreDbContext context, User user, CancellationToken cancellationToken)
    {
        var grants = await context.Set<UserPermissionGrantEntity>()
            .AsNoTracking()
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

    private static async Task SyncRoleAssignmentsAsync(EFCoreDbContext context, User user, CancellationToken cancellationToken)
    {
        // Get existing assignments from DB
        var existingAssignments = await context.Set<UserRoleAssignmentEntity>()
            .Where(a => a.UserId == user.Id)
            .ToListAsync(cancellationToken);

        // Get current assignments from domain model
        var currentAssignments = user.RoleAssignments;

        // Find assignments to remove
        var toRemove = existingAssignments
            .Where(existing => !currentAssignments.Any(current => current.RoleId == existing.RoleId))
            .ToList();

        // Find assignments to add
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

        // Find assignments to update (parameter values may have changed)
        var toUpdate = existingAssignments
            .Where(existing => currentAssignments.Any(current => 
                current.RoleId == existing.RoleId && 
                SerializeParameterValues(current.ParameterValues) != existing.ParameterValuesJson))
            .ToList();

        foreach (var update in toUpdate)
        {
            var current = currentAssignments.First(c => c.RoleId == update.RoleId);
            update.ParameterValuesJson = SerializeParameterValues(current.ParameterValues);
        }

        context.Set<UserRoleAssignmentEntity>().RemoveRange(toRemove);
        context.Set<UserRoleAssignmentEntity>().AddRange(toAdd);
    }

    private static async Task SyncPermissionGrantsAsync(EFCoreDbContext context, User user, CancellationToken cancellationToken)
    {
        // Get existing grants from DB
        var existingGrants = await context.Set<UserPermissionGrantEntity>()
            .Where(g => g.UserId == user.Id)
            .ToListAsync(cancellationToken);

        // Get current grants from domain model
        var currentGrants = user.PermissionGrants;

        // Find grants to remove (identifier AND type must match to be considered same grant)
        var toRemove = existingGrants
            .Where(existing => !currentGrants.Any(current => 
                string.Equals(current.Identifier, existing.PermissionIdentifier, StringComparison.Ordinal) &&
                current.Type == existing.Type))
            .ToList();

        // Find grants to add
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
            return JsonSerializer.Deserialize(json, EFCoreServerIdentityJsonContext.Default.DictionaryStringString);
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

        var dict = parameterValues as Dictionary<string, string?> ?? new Dictionary<string, string?>(parameterValues);
        return JsonSerializer.Serialize(dict, EFCoreServerIdentityJsonContext.Default.DictionaryStringString);
    }
}

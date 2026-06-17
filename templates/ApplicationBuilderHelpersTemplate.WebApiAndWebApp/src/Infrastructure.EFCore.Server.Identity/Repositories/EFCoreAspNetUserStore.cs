using Domain.Identity.Entities;
using Domain.Identity.Models;
using Infrastructure.EFCore.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.EFCore.Server.Identity.Repositories;

internal sealed class EFCoreAspNetUserStore(IDbContextFactory<EFCoreDbContext> contextFactory) :
    IUserStore<User>,
    IUserPasswordStore<User>,
    IUserEmailStore<User>,
    IUserPhoneNumberStore<User>,
    IUserTwoFactorStore<User>,
    IUserLockoutStore<User>,
    IUserAuthenticatorKeyStore<User>,
    IUserTwoFactorRecoveryCodeStore<User>
{
    public void Dispose()
    {
        // No resources to dispose - DbContext is created per-operation
    }

    public async Task<IdentityResult> CreateAsync(User user, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        context.Set<User>().Add(user);
        await context.SaveChangesWithExceptionHandlingAsync("User", user.UserName ?? user.Id.ToString(), cancellationToken);
        return IdentityResult.Success;
    }

    public async Task<IdentityResult> DeleteAsync(User user, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var existing = await context.Set<User>().FindAsync([user.Id], cancellationToken);
        if (existing is not null)
        {
            context.Set<User>().Remove(existing);
            await context.SaveChangesWithExceptionHandlingAsync(cancellationToken);
        }
        return IdentityResult.Success;
    }

    public async Task<User?> FindByIdAsync(string userId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(userId, out var id))
        {
            return null;
        }

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Set<User>().FindAsync([id], cancellationToken);
    }

    public async Task<User?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Set<User>()
            .FirstOrDefaultAsync(u => u.NormalizedUserName == normalizedUserName, cancellationToken);
    }

    public Task<string?> GetNormalizedUserNameAsync(User user, CancellationToken cancellationToken)
        => Task.FromResult(user.NormalizedUserName);

    public Task<string> GetUserIdAsync(User user, CancellationToken cancellationToken)
        => Task.FromResult(user.Id.ToString());

    public Task<string?> GetUserNameAsync(User user, CancellationToken cancellationToken)
        => Task.FromResult(user.UserName);

    public Task SetNormalizedUserNameAsync(User user, string? normalizedName, CancellationToken cancellationToken)
    {
        // Only set if non-null - normalized username comes from Identity framework
        if (!string.IsNullOrWhiteSpace(normalizedName))
        {
            user.SetNormalizedUserName(normalizedName);
        }
        return Task.CompletedTask;
    }

    public Task SetUserNameAsync(User user, string? userName, CancellationToken cancellationToken)
    {
        // Only set if non-null - username may be null for anonymous users
        if (!string.IsNullOrWhiteSpace(userName))
        {
            user.SetUserName(userName);
        }
        return Task.CompletedTask;
    }

    public async Task<IdentityResult> UpdateAsync(User user, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        context.Set<User>().Update(user);
        await context.SaveChangesWithExceptionHandlingAsync("User", user.UserName ?? user.Id.ToString(), cancellationToken);
        return IdentityResult.Success;
    }

    // IUserPasswordStore
    public Task SetPasswordHashAsync(User user, string? passwordHash, CancellationToken cancellationToken)
    {
        user.SetPasswordHash(passwordHash);
        return Task.CompletedTask;
    }

    public Task<string?> GetPasswordHashAsync(User user, CancellationToken cancellationToken)
        => Task.FromResult(user.PasswordHash);

    public Task<bool> HasPasswordAsync(User user, CancellationToken cancellationToken)
        => Task.FromResult(!string.IsNullOrEmpty(user.PasswordHash));

    // IUserEmailStore
    public Task SetEmailAsync(User user, string? email, CancellationToken cancellationToken)
    {
        user.SetEmail(email);
        return Task.CompletedTask;
    }

    public Task<string?> GetEmailAsync(User user, CancellationToken cancellationToken)
        => Task.FromResult(user.Email);

    public Task<bool> GetEmailConfirmedAsync(User user, CancellationToken cancellationToken)
        => Task.FromResult(user.EmailConfirmed);

    public Task SetEmailConfirmedAsync(User user, bool confirmed, CancellationToken cancellationToken)
    {
        user.SetEmailConfirmed(confirmed);
        return Task.CompletedTask;
    }

    public async Task<User?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Set<User>()
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);
    }

    public Task<string?> GetNormalizedEmailAsync(User user, CancellationToken cancellationToken)
        => Task.FromResult(user.NormalizedEmail);

    public Task SetNormalizedEmailAsync(User user, string? normalizedEmail, CancellationToken cancellationToken)
    {
        user.SetNormalizedEmail(normalizedEmail);
        return Task.CompletedTask;
    }

    // IUserPhoneNumberStore
    public Task SetPhoneNumberAsync(User user, string? phoneNumber, CancellationToken cancellationToken)
    {
        user.SetPhoneNumber(phoneNumber);
        return Task.CompletedTask;
    }

    public Task<string?> GetPhoneNumberAsync(User user, CancellationToken cancellationToken)
        => Task.FromResult(user.PhoneNumber);

    public Task<bool> GetPhoneNumberConfirmedAsync(User user, CancellationToken cancellationToken)
        => Task.FromResult(user.PhoneNumberConfirmed);

    public Task SetPhoneNumberConfirmedAsync(User user, bool confirmed, CancellationToken cancellationToken)
    {
        user.SetPhoneNumberConfirmed(confirmed);
        return Task.CompletedTask;
    }

    // IUserTwoFactorStore
    public Task SetTwoFactorEnabledAsync(User user, bool enabled, CancellationToken cancellationToken)
    {
        user.SetTwoFactorEnabled(enabled);
        return Task.CompletedTask;
    }

    public Task<bool> GetTwoFactorEnabledAsync(User user, CancellationToken cancellationToken)
        => Task.FromResult(user.TwoFactorEnabled);

    // IUserLockoutStore
    public Task<DateTimeOffset?> GetLockoutEndDateAsync(User user, CancellationToken cancellationToken)
        => Task.FromResult(user.LockoutEnd);

    public Task SetLockoutEndDateAsync(User user, DateTimeOffset? lockoutEnd, CancellationToken cancellationToken)
    {
        user.SetLockoutEnd(lockoutEnd);
        return Task.CompletedTask;
    }

    public Task<int> IncrementAccessFailedCountAsync(User user, CancellationToken cancellationToken)
    {
        user.IncrementAccessFailedCount();
        return Task.FromResult(user.AccessFailedCount);
    }

    public Task ResetAccessFailedCountAsync(User user, CancellationToken cancellationToken)
    {
        user.ResetAccessFailedCount();
        return Task.CompletedTask;
    }

    public Task<int> GetAccessFailedCountAsync(User user, CancellationToken cancellationToken)
        => Task.FromResult(user.AccessFailedCount);

    public Task<bool> GetLockoutEnabledAsync(User user, CancellationToken cancellationToken)
        => Task.FromResult(user.LockoutEnabled);

    public Task SetLockoutEnabledAsync(User user, bool enabled, CancellationToken cancellationToken)
    {
        user.SetLockoutEnabled(enabled);
        return Task.CompletedTask;
    }

    // IUserAuthenticatorKeyStore
    public Task SetAuthenticatorKeyAsync(User user, string key, CancellationToken cancellationToken)
    {
        user.SetAuthenticatorKey(key);
        return Task.CompletedTask;
    }

    public Task<string?> GetAuthenticatorKeyAsync(User user, CancellationToken cancellationToken)
        => Task.FromResult(user.AuthenticatorKey);

    // IUserTwoFactorRecoveryCodeStore
    public Task ReplaceCodesAsync(User user, IEnumerable<string> recoveryCodes, CancellationToken cancellationToken)
    {
        user.SetRecoveryCodes(string.Join(";", recoveryCodes));
        return Task.CompletedTask;
    }

    public Task<bool> RedeemCodeAsync(User user, string code, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(user.RecoveryCodes))
        {
            return Task.FromResult(false);
        }

        var codes = user.RecoveryCodes.Split(';').ToList();
        if (codes.Remove(code))
        {
            user.SetRecoveryCodes(codes.Count > 0 ? string.Join(";", codes) : null);
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    public Task<int> CountCodesAsync(User user, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(user.RecoveryCodes))
        {
            return Task.FromResult(0);
        }

        return Task.FromResult(user.RecoveryCodes.Split(';').Length);
    }
}

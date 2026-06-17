using Domain.Authorization.Services;
using Domain.Authorization.ValueObjects;
using Domain.Shared.Models;

namespace Domain.Identity.ValueObjects;

public class UserSession : ValueObject
{
    public Guid UserId { get; }
    public string? Username { get; }
    public IReadOnlyCollection<ScopeDirective> Scope { get; }
    public IReadOnlyCollection<string> PermissionIdentifiers { get; }
    public IReadOnlyCollection<string> RoleCodes { get; }
    public DateTimeOffset IssuedAt { get; }
    public DateTimeOffset ExpiresAt { get; }
    public bool IsAnonymous { get; }

    protected UserSession(
        Guid userId,
        string? username,
        IReadOnlyCollection<ScopeDirective> scope,
        IReadOnlyCollection<string> permissionIdentifiers,
        IReadOnlyCollection<string> roleCodes,
        DateTimeOffset issuedAt,
        DateTimeOffset expiresAt,
        bool isAnonymous = false)
    {
        UserId = userId;
        Username = username;
        Scope = scope;
        PermissionIdentifiers = permissionIdentifiers;
        RoleCodes = roleCodes;
        IssuedAt = issuedAt;
        ExpiresAt = expiresAt;
        IsAnonymous = isAnonymous;
    }

    public static UserSession Create(
        Guid userId,
        string? username,
        IReadOnlyCollection<string> roleCodes,
        IReadOnlyCollection<string> permissionIdentifiers,
        bool isAnonymous = false)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User identifier cannot be empty.", nameof(userId));
        }

        if (!isAnonymous && string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("Username cannot be empty for non-anonymous users.", nameof(username));
        }

        var now = DateTimeOffset.UtcNow;
        return new UserSession(
            userId,
            username?.Trim(),
            Array.Empty<ScopeDirective>(),
            permissionIdentifiers ?? Array.Empty<string>(),
            roleCodes ?? Array.Empty<string>(),
            now,
            now.AddHours(1),
            isAnonymous);
    }

    public static UserSession Create(
        Guid userId,
        string? username,
        IEnumerable<ScopeDirective> scope,
        DateTimeOffset issuedAt,
        DateTimeOffset expiresAt,
        IEnumerable<string>? roleCodes = null,
        bool isAnonymous = false)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User identifier cannot be empty.", nameof(userId));
        }

        if (!isAnonymous && string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("Username cannot be empty for non-anonymous users.", nameof(username));
        }

        if (expiresAt <= issuedAt)
        {
            throw new ArgumentException("Session expiration must be greater than issuance time.", nameof(expiresAt));
        }

        var scopeList = scope?
            .Where(static s => s != null)
            .Distinct()
            .ToArray() ?? Array.Empty<ScopeDirective>();

        // Generate legacy permission identifiers from scope for backward compatibility
        var permissions = scopeList
            .Select(static s => s.ToString())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static p => p, StringComparer.Ordinal)
            .ToArray();

        var roles = roleCodes?
            .Where(static code => !string.IsNullOrWhiteSpace(code))
            .Select(static code => code.Trim())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static code => code, StringComparer.Ordinal)
            .ToArray() ?? Array.Empty<string>();

        return new UserSession(userId, username?.Trim(), scopeList, permissions, roles, issuedAt, expiresAt, isAnonymous);
    }

    public static UserSession CreateLegacy(
        Guid userId,
        string? username,
        IEnumerable<string> permissionIdentifiers,
        DateTimeOffset issuedAt,
        DateTimeOffset expiresAt,
        IEnumerable<string>? roleCodes = null,
        bool isAnonymous = false)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User identifier cannot be empty.", nameof(userId));
        }

        if (!isAnonymous && string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("Username cannot be empty for non-anonymous users.", nameof(username));
        }

        if (expiresAt <= issuedAt)
        {
            throw new ArgumentException("Session expiration must be greater than issuance time.", nameof(expiresAt));
        }

        var permissions = permissionIdentifiers?
            .Where(static p => !string.IsNullOrWhiteSpace(p))
            .Select(static p => p.Trim())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static p => p, StringComparer.Ordinal)
            .ToArray() ?? Array.Empty<string>();

        var roles = roleCodes?
            .Where(static code => !string.IsNullOrWhiteSpace(code))
            .Select(static code => code.Trim())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static code => code, StringComparer.Ordinal)
            .ToArray() ?? Array.Empty<string>();

        // Empty scope for legacy sessions
        return new UserSession(userId, username?.Trim(), Array.Empty<ScopeDirective>(), permissions, roles, issuedAt, expiresAt, isAnonymous);
    }

    public bool HasPermission(string permissionPath, IReadOnlyDictionary<string, string>? requestParameters = null)
    {
        return ScopeEvaluator.HasPermission(Scope, permissionPath, requestParameters);
    }

    public bool HasAnyPermission(IEnumerable<string> permissionPaths, IReadOnlyDictionary<string, string>? requestParameters = null)
    {
        return ScopeEvaluator.HasAnyPermission(Scope, permissionPaths, requestParameters);
    }

    public bool HasAllPermissions(IEnumerable<string> permissionPaths, IReadOnlyDictionary<string, string>? requestParameters = null)
    {
        return ScopeEvaluator.HasAllPermissions(Scope, permissionPaths, requestParameters);
    }

    public IReadOnlyDictionary<string, string> GetParameters(string permissionPath)
    {
        return ScopeEvaluator.GetParameters(Scope, permissionPath);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return UserId;
        yield return Username ?? string.Empty;
        yield return IssuedAt;
        yield return ExpiresAt;
    }
}

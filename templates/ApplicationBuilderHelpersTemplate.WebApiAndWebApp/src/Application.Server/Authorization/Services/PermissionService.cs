using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Domain.Authorization.Constants;
using Domain.Authorization.Entities;
using Domain.Shared.Constants;
using Domain.Shared.Exceptions;
using Application.Server.Authorization.Interfaces.Inbound;
using Application.Server.Authorization.Interfaces.Outbound;
using Domain.Authorization.Enums;
using Domain.Authorization.Models;
using Domain.Authorization.Services;
using Domain.Authorization.ValueObjects;
using Domain.Identity.Enums;
using TokenClaimTypes = Domain.Identity.Constants.TokenClaimTypes;

namespace Application.Server.Authorization.Services;

internal sealed class PermissionService(
    ITokenProvider tokenProvider,
    IRoleRepository roleRepository) : IPermissionService
{
    private const string ScopeClaimType = "scope";

    private static readonly ReadOnlyDictionary<string, HashSet<string>> ReachableParameterLookup;
    private static readonly HashSet<string> EmptyParameterNameSet = new(StringComparer.Ordinal);

    static PermissionService()
    {
        ReachableParameterLookup = BuildReachableParameterLookup(PermissionCache.TreeRoots);
    }

    public async Task<string> GenerateTokenWithPermissionsAsync(
        string userId,
        string? username,
        IEnumerable<string> permissionIdentifiers,
        IEnumerable<Claim>? additionalClaims = null,
        DateTimeOffset? expiration = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(userId);

        var normalizedPermissions = NormalizeAndValidate(permissionIdentifiers, allowEmpty: true);
        var additionalClaimSet = additionalClaims?.ToArray();

        return await tokenProvider.GenerateTokenWithScopesAsync(
            userId: userId,
            username: username ?? string.Empty,
            scopes: normalizedPermissions,
            additionalClaims: additionalClaimSet,
            expiration: expiration,
            cancellationToken: cancellationToken);
    }

    public async Task<string> GenerateApiKeyTokenWithPermissionsAsync(
        string apiKeyName,
        IEnumerable<string> permissionIdentifiers,
        IEnumerable<Claim>? additionalClaims = null,
        DateTimeOffset? expiration = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(apiKeyName);

        var normalizedPermissions = NormalizeAndValidate(permissionIdentifiers, allowEmpty: true);
        var additionalClaimSet = additionalClaims?.ToArray();

        return await tokenProvider.GenerateApiKeyTokenAsync(
            apiKeyName: apiKeyName,
            scopes: normalizedPermissions,
            additionalClaims: additionalClaimSet,
            expiration: expiration,
            cancellationToken: cancellationToken);
    }

    public async Task<string> GenerateTokenWithScopeAsync(
        string userId,
        string? username,
        IEnumerable<ScopeDirective> scopeDirectives,
        IEnumerable<Claim>? additionalClaims = null,
        DateTimeOffset? expiration = null,
        TokenType tokenType = TokenType.Access,
        string? tokenId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(userId);

        // Convert scope directives to their string representation
        var scopes = scopeDirectives?
            .Where(static d => d != null)
            .Select(static d => d.ToString())
            .ToArray() ?? Array.Empty<string>();

        var additionalClaimSet = additionalClaims?.ToArray();

        return await tokenProvider.GenerateTokenWithScopesAsync(
            userId: userId,
            username: username ?? string.Empty,
            scopes: scopes,
            additionalClaims: additionalClaimSet,
            expiration: expiration,
            tokenType: tokenType,
            tokenId: tokenId,
            cancellationToken: cancellationToken);
    }

    public Task<IReadOnlyCollection<Permission>> GetPermissionTreeAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(PermissionCache.TreeRoots);

    public Task<IReadOnlyCollection<string>> GetAllPermissionIdentifiersAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(PermissionCache.AssignableIdentifiers);

    public Task<bool> ValidatePermissionsAsync(IEnumerable<string> permissionIdentifiers, CancellationToken cancellationToken = default)
    {
        if (permissionIdentifiers is null)
        {
            return Task.FromResult(false);
        }

        foreach (var identifier in permissionIdentifiers)
        {
            if (string.IsNullOrWhiteSpace(identifier))
            {
                return Task.FromResult(false);
            }

            if (!Permission.TryParseIdentifier(identifier, out var parsed))
            {
                return Task.FromResult(false);
            }

            if (!PermissionCache.ByPath.TryGetValue(parsed.Canonical, out var permission))
            {
                return Task.FromResult(false);
            }

            if (permission.AccessCategory == PermissionAccessCategory.Unspecified)
            {
                return Task.FromResult(false);
            }

            if (!AreParametersValid(permission, parsed.Parameters, out _))
            {
                return Task.FromResult(false);
            }
        }

        return Task.FromResult(true);
    }

    public Task<IReadOnlyCollection<Permission>> ResolvePermissionsAsync(
        IEnumerable<string> permissionIdentifiers,
        CancellationToken cancellationToken = default)
    {
        var normalizedPermissions = NormalizeAndValidate(permissionIdentifiers, allowEmpty: true);
        if (normalizedPermissions.Length == 0)
        {
            return Task.FromResult<IReadOnlyCollection<Permission>>([]);
        }

        var resolved = new List<Permission>(normalizedPermissions.Length);
        foreach (var identifier in normalizedPermissions)
        {
            if (!Permission.TryParseIdentifier(identifier, out var parsed))
            {
                continue;
            }

            if (PermissionCache.ByPath.TryGetValue(parsed.Canonical, out var permission))
            {
                resolved.Add(permission);
            }
        }

        return Task.FromResult<IReadOnlyCollection<Permission>>(resolved.Count == 0
            ? Array.Empty<Permission>()
            : [.. resolved]);
    }

    public async Task<bool> HasPermissionAsync(ClaimsPrincipal principal, string permissionIdentifier, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(principal);

        if (string.IsNullOrWhiteSpace(permissionIdentifier))
        {
            return false;
        }

        var trimmed = permissionIdentifier.Trim();
        if (!Permission.TryParseIdentifier(trimmed, out var parsed))
        {
            return false;
        }

        if (!PermissionCache.ByPath.TryGetValue(parsed.Canonical, out var permission))
        {
            return false;
        }

        if (!AreParametersValid(permission, parsed.Parameters, out _))
        {
            return false;
        }

        // Resolve roles at runtime and evaluate
        var scope = await ResolveScopeDirectivesAsync(principal, cancellationToken).ConfigureAwait(false);
        return ScopeEvaluator.HasPermission(scope, parsed.Canonical, parsed.Parameters);
    }

    public async Task<bool> HasAnyPermissionAsync(ClaimsPrincipal principal, IEnumerable<string> permissionIdentifiers, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(principal);

        if (permissionIdentifiers is null)
        {
            return false;
        }

        var scope = await ResolveScopeDirectivesAsync(principal, cancellationToken).ConfigureAwait(false);

        foreach (var identifier in permissionIdentifiers)
        {
            if (string.IsNullOrWhiteSpace(identifier))
            {
                continue;
            }

            var trimmed = identifier.Trim();
            if (!Permission.TryParseIdentifier(trimmed, out var parsed))
            {
                continue;
            }

            if (!PermissionCache.ByPath.TryGetValue(parsed.Canonical, out var permission))
            {
                continue;
            }

            if (!AreParametersValid(permission, parsed.Parameters, out _))
            {
                continue;
            }

            if (ScopeEvaluator.HasPermission(scope, parsed.Canonical, parsed.Parameters))
            {
                return true;
            }
        }

        return false;
    }

    public async Task<bool> HasAllPermissionsAsync(ClaimsPrincipal principal, IEnumerable<string> permissionIdentifiers, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(principal);

        if (permissionIdentifiers is null)
        {
            return false;
        }

        var identifiers = permissionIdentifiers
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .ToList();

        if (identifiers.Count == 0)
        {
            return false;
        }

        var scope = await ResolveScopeDirectivesAsync(principal, cancellationToken).ConfigureAwait(false);

        foreach (var identifier in identifiers)
        {
            if (!Permission.TryParseIdentifier(identifier, out var parsed))
            {
                return false;
            }

            if (!PermissionCache.ByPath.TryGetValue(parsed.Canonical, out var permission))
            {
                return false;
            }

            if (!AreParametersValid(permission, parsed.Parameters, out _))
            {
                return false;
            }

            if (!ScopeEvaluator.HasPermission(scope, parsed.Canonical, parsed.Parameters))
            {
                return false;
            }
        }

        return true;
    }

    private static string[] NormalizeAndValidate(IEnumerable<string> permissionIdentifiers, bool allowEmpty)
    {
        if (permissionIdentifiers is null)
        {
            return [];
        }

        var unique = new HashSet<string>(StringComparer.Ordinal);
        var ordered = new List<string>();

        foreach (var rawIdentifier in permissionIdentifiers)
        {
            if (string.IsNullOrWhiteSpace(rawIdentifier))
            {
                throw new ArgumentException("Permission identifiers cannot contain null or whitespace entries.", nameof(permissionIdentifiers));
            }

            Permission.ParsedIdentifier parsed;
            try
            {
                parsed = Permission.ParseIdentifier(rawIdentifier);
            }
            catch (FormatException ex)
            {
                throw new ArgumentException($"Permission identifier '{rawIdentifier}' has an invalid format: {ex.Message}", nameof(permissionIdentifiers), ex);
            }

            if (!PermissionCache.ByPath.TryGetValue(parsed.Canonical, out var permission))
            {
                throw new ArgumentException($"Unknown permission identifier '{parsed.Identifier}'.", nameof(permissionIdentifiers));
            }

            if (permission.AccessCategory == PermissionAccessCategory.Unspecified)
            {
                throw new ArgumentException($"Permission identifier '{parsed.Identifier}' is not assignable. Select a specific scope or operation.", nameof(permissionIdentifiers));
            }

            if (!AreParametersValid(permission, parsed.Parameters, out var invalidParameter))
            {
                throw new ArgumentException($"Permission identifier '{parsed.Identifier}' specifies unsupported parameter '{invalidParameter}'.", nameof(permissionIdentifiers));
            }

            if (unique.Add(parsed.Identifier))
            {
                ordered.Add(parsed.Identifier);
            }
        }

        if (!allowEmpty && ordered.Count == 0)
        {
            throw new ArgumentException("At least one permission identifier must be provided.", nameof(permissionIdentifiers));
        }

        return ordered.Count == 0
            ? []
            : [.. ordered];
    }

    private static List<ScopeDirective> ExtractScopeDirectives(ClaimsPrincipal principal)
    {
        var directives = new List<ScopeDirective>();

        foreach (var claim in principal.Claims)
        {
            if (string.IsNullOrWhiteSpace(claim.Value))
            {
                continue;
            }

            if (!string.Equals(claim.Type, ScopeClaimType, StringComparison.Ordinal))
            {
                continue;
            }

            var scopes = claim.Value.Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var scope in scopes)
            {
                if (ScopeDirective.TryParse(scope, out var directive))
                {
                    directives.Add(directive!);
                }
            }
        }

        return directives;
    }

    private async Task<List<ScopeDirective>> ResolveScopeDirectivesAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var directives = new List<ScopeDirective>();

        // First, extract any direct scope claims (for backward compatibility)
        directives.AddRange(ExtractScopeDirectives(principal));

        // Parse role claims with inline parameters (format: "ROLE_CODE;param1=value1;param2=value2")
        // RFC 9068 Section 2.2.3.1 / RFC 7643 Section 4.1.2 specify "roles" (plural)
        var parsedRoles = principal.Claims
            .Where(c => string.Equals(c.Type, TokenClaimTypes.Roles, StringComparison.Ordinal))
            .Select(c => c.Value)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => Role.TryParseRoleClaim(v, out var parsed) ? (Role.ParsedRoleClaim?)parsed : null)
            .Where(p => p.HasValue)
            .Select(p => p!.Value)
            .ToList();

        if (parsedRoles.Count == 0)
        {
            return directives;
        }

        var roleCodes = parsedRoles.Select(p => p.Code).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        var roles = await roleRepository.GetByCodesAsync(roleCodes, cancellationToken).ConfigureAwait(false);
        var roleIndex = roles.ToDictionary(r => r.Code, StringComparer.OrdinalIgnoreCase);

        foreach (var parsedRole in parsedRoles)
        {
            if (!roleIndex.TryGetValue(parsedRole.Code, out var role))
            {
                continue;
            }

            var parameterValues = parsedRole.Parameters.ToDictionary(
                kvp => kvp.Key,
                kvp => (string?)kvp.Value,
                StringComparer.Ordinal);

            try
            {
                var roleDirectives = role.ExpandScope(parameterValues);
                directives.AddRange(roleDirectives);
            }
            catch (Domain.Shared.Exceptions.DomainException)
            {
                // Role requires parameters that weren't provided in the token
                // Try to expand individual templates that don't require missing parameters
                foreach (var template in role.ScopeTemplates)
                {
                    // Check if all required parameters are available
                    var missingParams = template.RequiredParameters
                        .Where(p => !parameterValues.ContainsKey(p) || parameterValues[p] is null)
                        .ToList();

                    if (missingParams.Count == 0)
                    {
                        // All parameters available, expand this template
                        try
                        {
                            var directive = template.Expand(parameterValues);
                            directives.Add(directive);
                        }
                        catch
                        {
                            // Skip templates that fail to expand
                        }
                    }
                    // else: Skip templates with missing parameters
                }
            }
        }

        return directives;
    }

    private static HashSet<string> CollectRelevantParameterNames(Permission permission)
        => new(permission.GetParameterHierarchy(), StringComparer.Ordinal);

    private static bool AreParametersValid(Permission permission, IReadOnlyDictionary<string, string> parameters, out string? invalidParameter)
    {
        if (parameters.Count == 0)
        {
            invalidParameter = null;
            return true;
        }

        if (permission.Identifier is "_read" or "_write" && permission.Parent is null)
        {
            invalidParameter = null;
            return true;
        }

        var allowedAncestors = CollectRelevantParameterNames(permission);
        var reachable = GetReachableParameterNames(permission);

        if (allowedAncestors.Count == 0 && reachable.Count == 0)
        {
            invalidParameter = null;
            return true;
        }

        foreach (var parameter in parameters.Keys)
        {
            if (allowedAncestors.Contains(parameter))
            {
                continue;
            }

            if (reachable.Contains(parameter))
            {
                continue;
            }

            invalidParameter = parameter;
            return false;
        }

        invalidParameter = null;
        return true;
    }

    private static ReadOnlyDictionary<string, HashSet<string>> BuildReachableParameterLookup(IEnumerable<Permission> roots)
    {
        var cache = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        foreach (var root in roots)
        {
            if (root.Identifier is "_read" or "_write")
            {
                continue;
            }

            ComputeReachableParameters(root, cache);
        }

        foreach (var permission in roots.SelectMany(static root => root.Traverse()))
        {
            if (cache.ContainsKey(permission.Path))
            {
                continue;
            }

            if (permission.Identifier is "_read" or "_write")
            {
                var parent = permission.Parent;
                if (parent is not null && cache.TryGetValue(parent.Path, out var parentParameters))
                {
                    cache[permission.Path] = parentParameters;
                }
                else
                {
                    cache[permission.Path] = EmptyParameterNameSet;
                }
            }
            else
            {
                cache[permission.Path] = new HashSet<string>(permission.Parameters, StringComparer.Ordinal);
            }
        }

        return new ReadOnlyDictionary<string, HashSet<string>>(cache);
    }

    private static HashSet<string> ComputeReachableParameters(Permission permission, Dictionary<string, HashSet<string>> cache)
    {
        if (cache.TryGetValue(permission.Path, out var existing))
        {
            return existing;
        }

        var names = new HashSet<string>(permission.Parameters, StringComparer.Ordinal);

        foreach (var child in permission.Permissions)
        {
            if (child.Identifier is "_read" or "_write")
            {
                continue;
            }

            var childNames = ComputeReachableParameters(child, cache);
            names.UnionWith(childNames);
        }

        cache[permission.Path] = names;
        return names;
    }

    private static HashSet<string> GetReachableParameterNames(Permission permission)
    {
        if (ReachableParameterLookup.TryGetValue(permission.Path, out var parameters))
        {
            return parameters;
        }

        return EmptyParameterNameSet;
    }
}

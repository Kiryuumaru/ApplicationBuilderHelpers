using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Domain.Authorization.Constants;
using Domain.Authorization.Entities;
using Domain.Authorization.Models;
using Domain.Authorization.Utilities;
using Domain.Authorization.ValueObjects;
using TokenClaimTypes = Domain.Identity.Constants.TokenClaimTypes;

namespace Application.Authorization.Services;

internal sealed partial class PermissionService
{
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

    private static List<ScopeDirective> ExtractScopeDirectives(ClaimsPrincipal principal)
    {
        var directives = new List<ScopeDirective>();

        foreach (var claim in principal.Claims)
        {
            if (string.IsNullOrWhiteSpace(claim.Value))
            {
                continue;
            }

            if (!string.Equals(claim.Type, TokenClaimTypes.Scope, StringComparison.Ordinal))
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
            .Select(v => Role.TryParseRoleClaim(v, out var parsed) ? (ParsedRoleClaim?)parsed : null)
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
}

using Domain.Shared.Exceptions;
using Infrastructure.Identity.Interfaces;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using TokenClaimTypes = Domain.Identity.Constants.TokenClaimTypes;

namespace Infrastructure.Identity.Services;

internal sealed partial class JwtTokenService
{
    public async Task<string> MutateToken(
        string token,
        IEnumerable<string>? scopesToAdd = null,
        IEnumerable<string>? scopesToRemove = null,
        IEnumerable<Claim>? claimsToAdd = null,
        IEnumerable<Claim>? claimsToRemove = null,
        IEnumerable<string>? claimTypesToRemove = null,
        DateTimeOffset? expiration = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("Token must be provided.", nameof(token));
        }

        var principal = await ValidateToken(token, expectedType: null, cancellationToken) ?? throw new SecurityTokenException("Token validation failed.");

        JwtSecurityToken jwtToken;
        try
        {
            jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(token);
        }
        catch (Exception ex)
        {
            throw new SecurityTokenException("Token validation failed.", ex);
        }

        var userId = principal.FindFirstValue(TokenClaimTypes.Subject);
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ValidationException("Token does not contain a subject identifier claim.");
        }

        var username = principal.FindFirstValue(TokenClaimTypes.Name) ?? principal.Identity?.Name ?? userId;

        var mutableClaims = principal.Claims
            .Where(static claim => !IsReservedIdentityClaimType(claim.Type)
                && !string.Equals(claim.Type, TokenClaimTypes.Scope, StringComparison.Ordinal))
            .Select(CloneClaim)
            .ToList();

        var (existingScopes, scopeSet) = ExtractScopes(principal);

        if (scopesToRemove is not null)
        {
            foreach (var scope in scopesToRemove)
            {
                if (string.IsNullOrWhiteSpace(scope))
                {
                    continue;
                }

                var trimmed = scope.Trim();

                if (!scopeSet.Remove(trimmed))
                {
                    continue;
                }

                existingScopes.RemoveAll(existing => string.Equals(existing, trimmed, StringComparison.Ordinal));
            }
        }

        if (scopesToAdd is not null)
        {
            foreach (var scope in scopesToAdd)
            {
                if (string.IsNullOrWhiteSpace(scope))
                {
                    continue;
                }

                var trimmed = scope.Trim();

                if (scopeSet.Add(trimmed))
                {
                    existingScopes.Add(trimmed);
                }
            }
        }

        if (claimTypesToRemove is not null)
        {
            foreach (var type in claimTypesToRemove)
            {
                if (string.IsNullOrWhiteSpace(type))
                {
                    continue;
                }

                var trimmed = type.Trim();
                if (IsReservedIdentityClaimType(trimmed) || string.Equals(trimmed, TokenClaimTypes.Scope, StringComparison.Ordinal))
                {
                    throw new ValidationException($"Claim type '{trimmed}' cannot be removed.");
                }

                mutableClaims.RemoveAll(claim => string.Equals(claim.Type, trimmed, StringComparison.Ordinal));
            }
        }

        if (claimsToRemove is not null)
        {
            foreach (var claim in claimsToRemove)
            {
                if (claim is null || string.IsNullOrWhiteSpace(claim.Type))
                {
                    continue;
                }

                if (IsReservedIdentityClaimType(claim.Type))
                {
                    throw new ValidationException($"Claim type '{claim.Type}' cannot be removed.");
                }

                if (string.Equals(claim.Type, TokenClaimTypes.Scope, StringComparison.Ordinal))
                {
                    throw new ValidationException("Scope claims must be removed via scopesToRemove.");
                }

                mutableClaims.RemoveAll(existing =>
                    string.Equals(existing.Type, claim.Type, StringComparison.Ordinal) &&
                    string.Equals(existing.Value, claim.Value, StringComparison.Ordinal));
            }
        }

        if (claimsToAdd is not null)
        {
            foreach (var claim in claimsToAdd)
            {
                if (claim is null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(claim.Type))
                {
                    throw new ArgumentException("Claims to add must define a type.", nameof(claimsToAdd));
                }

                if (IsReservedIdentityClaimType(claim.Type))
                {
                    throw new ValidationException($"Claim type '{claim.Type}' cannot be added.");
                }

                if (string.Equals(claim.Type, TokenClaimTypes.Scope, StringComparison.Ordinal))
                {
                    throw new ValidationException("Scope claims must be added via scopesToAdd.");
                }

                var alreadyPresent = mutableClaims.Any(existing =>
                    string.Equals(existing.Type, claim.Type, StringComparison.Ordinal) &&
                    string.Equals(existing.Value, claim.Value, StringComparison.Ordinal) &&
                    string.Equals(existing.ValueType, claim.ValueType, StringComparison.Ordinal));

                if (!alreadyPresent)
                {
                    mutableClaims.Add(CloneClaim(claim));
                }
            }
        }

        var now = DateTime.UtcNow;
        DateTimeOffset? effectiveExpiration = null;

        if (expiration.HasValue)
        {
            var normalizedOverride = NormalizeToUtc(expiration.Value.UtcDateTime);
            if (normalizedOverride == default)
            {
                throw new SecurityTokenException("Expiration override must provide a valid timestamp.");
            }

            if (normalizedOverride <= now)
            {
                throw new SecurityTokenException("Token expiration must be in the future.");
            }

            effectiveExpiration = new DateTimeOffset(normalizedOverride, TimeSpan.Zero);
        }
        else
        {
            var normalizedExpiration = NormalizeExpiration(jwtToken.ValidTo);
            if (normalizedExpiration is not null)
            {
                if (normalizedExpiration <= now)
                {
                    throw new SecurityTokenException("Token has already expired.");
                }

                effectiveExpiration = new DateTimeOffset(normalizedExpiration.Value, TimeSpan.Zero);
            }
        }

        return await GenerateToken(
            userId: userId,
            username: username,
            scopes: existingScopes,
            additionalClaims: mutableClaims,
            expiration: effectiveExpiration,
            cancellationToken: cancellationToken);
    }

    private static (List<string> Items, HashSet<string> Set) ExtractScopes(ClaimsPrincipal principal)
    {
        var items = new List<string>();
        var set = new HashSet<string>(StringComparer.Ordinal);

        foreach (var claim in principal.Claims)
        {
            if (!string.Equals(claim.Type, TokenClaimTypes.Scope, StringComparison.Ordinal))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(claim.Value))
            {
                continue;
            }

            var values = claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (values.Length == 0)
            {
                continue;
            }

            foreach (var value in values)
            {
                var trimmed = value.Trim();
                if (trimmed.Length == 0)
                {
                    continue;
                }

                if (set.Add(trimmed))
                {
                    items.Add(trimmed);
                }
            }
        }

        return (items, set);
    }

    private static DateTime? NormalizeExpiration(DateTime value)
    {
        if (value == DateTime.MinValue)
        {
            return null;
        }

        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            _ => value.ToUniversalTime()
        };
    }
}

using Application.Authorization.Models;
using Domain.Authorization.Constants;
using Infrastructure.Identity.Models;
using Domain.Identity.Enums;
using Infrastructure.Identity.Interfaces;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TokenClaimTypes = Domain.Identity.Constants.TokenClaimTypes;

namespace Infrastructure.Identity.Services;

internal sealed partial class JwtTokenService
{
    public async Task<string> GenerateToken(
        string userId,
        string username,
        IEnumerable<string>? scopes = null,
        IEnumerable<Claim>? additionalClaims = null,
        DateTimeOffset? expiration = null,
        TokenType tokenType = TokenType.Access,
        string? tokenId = null,
        CancellationToken cancellationToken = default)
    {
        var jwtConfiguration = await jwtConfigurationFactory.Value(cancellationToken);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfiguration.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(TokenClaimTypes.Subject, userId),
            new(TokenClaimTypes.Name, username),
            new(TokenClaimTypes.TokenId, tokenId ?? Guid.NewGuid().ToString()),
            new(TokenClaimTypes.IssuedAt, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        if (additionalClaims is not null)
        {
            foreach (var claim in additionalClaims)
            {
                if (claim is null)
                {
                    continue;
                }

                if (IsReservedIdentityClaimType(claim.Type))
                {
                    continue;
                }

                if (string.Equals(claim.Type, TokenClaimTypes.Scope, StringComparison.Ordinal))
                {
                    continue;
                }

                var duplicate = claims.Any(existing =>
                    string.Equals(existing.Type, claim.Type, StringComparison.Ordinal) &&
                    string.Equals(existing.Value, claim.Value, StringComparison.Ordinal) &&
                    string.Equals(existing.ValueType, claim.ValueType, StringComparison.Ordinal));

                if (!duplicate)
                {
                    claims.Add(CloneClaim(claim));
                }
            }
        }

        if (scopes is not null)
        {
            var seenScopes = new HashSet<string>(StringComparer.Ordinal);

            foreach (var scope in scopes)
            {
                if (string.IsNullOrWhiteSpace(scope))
                {
                    continue;
                }

                var trimmed = scope.Trim();

                if (seenScopes.Add(trimmed))
                {
                    claims.Add(new Claim(TokenClaimTypes.Scope, trimmed));
                }
            }
        }

        if (!claims.Any(claim => string.Equals(claim.Type, RbacConstants.VersionClaimType, StringComparison.Ordinal)))
        {
            claims.Add(new Claim(RbacConstants.VersionClaimType, RbacConstants.CurrentVersion));
        }

        var now = DateTime.UtcNow;
        DateTime expirationTime;

        if (expiration.HasValue)
        {
            var normalizedExpiration = NormalizeToUtc(expiration.Value.UtcDateTime);
            if (normalizedExpiration == default)
            {
                throw new SecurityTokenException("Expiration override must provide a valid timestamp.");
            }

            if (normalizedExpiration <= now)
            {
                throw new SecurityTokenException("Token expiration must be in the future.");
            }

            expirationTime = normalizedExpiration;
        }
        else
        {
            expirationTime = now.Add(jwtConfiguration.DefaultExpiration);
        }

        var tokenTypeValue = GetTokenTypeValue(tokenType);

        var header = new JwtHeader(credentials)
        {
            [TokenClaimTypes.TokenType] = tokenTypeValue
        };

        var payload = new JwtPayload(
            issuer: jwtConfiguration.Issuer,
            audience: jwtConfiguration.Audience,
            claims: claims,
            notBefore: null,
            expires: expirationTime);

        var token = new JwtSecurityToken(header, payload);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<TokenValidationParameters> GetTokenValidationParameters(CancellationToken cancellationToken = default)
    {
        var jwtConfiguration = await jwtConfigurationFactory.Value(cancellationToken);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfiguration.Secret));

        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtConfiguration.Issuer,
            ValidAudience = jwtConfiguration.Audience,
            IssuerSigningKey = key,
            ClockSkew = jwtConfiguration.ClockSkew
        };
    }
}

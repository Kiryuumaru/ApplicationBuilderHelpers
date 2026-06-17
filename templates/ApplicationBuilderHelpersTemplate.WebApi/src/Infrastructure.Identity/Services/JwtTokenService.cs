using Domain.Identity.Enums;
using Infrastructure.Identity.Interfaces;
using Infrastructure.Identity.Models;
using System.Security.Claims;
using TokenClaimTypes = Domain.Identity.Constants.TokenClaimTypes;

namespace Infrastructure.Identity.Services;

internal sealed partial class JwtTokenService(Lazy<Func<CancellationToken, Task<JwtConfiguration>>> jwtConfigurationFactory) : IJwtTokenService
{
    private static bool IsReservedIdentityClaimType(string claimType)
    {
        // Only filter claims that are ALWAYS added in GenerateToken base claims
        // Do NOT filter SessionId - it's only passed via additionalClaims
        return string.Equals(claimType, TokenClaimTypes.Subject, StringComparison.Ordinal)
            || string.Equals(claimType, TokenClaimTypes.Name, StringComparison.Ordinal)
            || string.Equals(claimType, TokenClaimTypes.TokenId, StringComparison.Ordinal)
            || string.Equals(claimType, TokenClaimTypes.IssuedAt, StringComparison.Ordinal);
    }

    private static Claim CloneClaim(Claim source)
    {
        var clone = new Claim(source.Type, source.Value, source.ValueType, source.Issuer, source.OriginalIssuer);
        foreach (var property in source.Properties)
        {
            clone.Properties[property.Key] = property.Value;
        }

        return clone;
    }

    private static DateTime NormalizeToUtc(DateTime value)
    {
        if (value == default)
        {
            return default;
        }

        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            _ => value
        };
    }

    private static string GetTokenTypeValue(TokenType tokenType)
    {
        return tokenType switch
        {
            TokenType.Access => TokenClaimTypes.TokenTypeValues.AccessToken,
            TokenType.Refresh => TokenClaimTypes.TokenTypeValues.RefreshToken,
            TokenType.ApiKey => TokenClaimTypes.TokenTypeValues.ApiKey,
            _ => throw new ArgumentOutOfRangeException(nameof(tokenType), tokenType, "Unknown token type")
        };
    }
}

using Domain.Identity.Enums;
using Infrastructure.Identity.Interfaces;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using TokenClaimTypes = Domain.Identity.Constants.TokenClaimTypes;

namespace Infrastructure.Identity.Services;

internal sealed partial class JwtTokenService
{
    public async Task<ClaimsPrincipal?> ValidateToken(
        string token,
        TokenType? expectedType = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler { MapInboundClaims = false };

            var validationParameters = await GetTokenValidationParameters(cancellationToken);

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

            if (validatedToken is JwtSecurityToken jwtToken)
            {
                var expirationUtc = jwtToken.ValidTo.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(jwtToken.ValidTo, DateTimeKind.Utc)
                    : jwtToken.ValidTo.ToUniversalTime();

                if (expirationUtc != default)
                {
                    var now = DateTime.UtcNow;
                    if (expirationUtc.Add(validationParameters.ClockSkew) < now)
                    {
                        throw new SecurityTokenExpiredException($"The token expired at {expirationUtc:O}.");
                    }
                }

                if (expectedType.HasValue)
                {
                    var expectedTypValue = GetTokenTypeValue(expectedType.Value);
                    var actualTypValue = jwtToken.Header.Typ;

                    if (!string.Equals(actualTypValue, expectedTypValue, StringComparison.Ordinal))
                    {
                        return null;
                    }
                }
            }

            return principal;
        }
        catch
        {
            return null;
        }
    }
}

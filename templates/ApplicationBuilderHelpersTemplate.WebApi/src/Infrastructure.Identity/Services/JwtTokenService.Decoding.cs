using Application.Authorization.Models;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Nodes;
using TokenClaimTypes = Domain.Identity.Constants.TokenClaimTypes;

namespace Infrastructure.Identity.Services;

internal sealed partial class JwtTokenService
{
    public Task<TokenInfo?> DecodeToken(string token, CancellationToken cancellationToken = default)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);

            var issuedAt = ExtractLifetime(jwtToken, TokenClaimTypes.IssuedAt, jwtToken.IssuedAt);
            var expiresAt = ExtractLifetime(jwtToken, TokenClaimTypes.ExpiresAt, jwtToken.ValidTo);

            var claims = new Dictionary<string, JsonNode?>(StringComparer.Ordinal);

            foreach (var claim in jwtToken.Claims)
            {
                var currentValue = ConvertToJsonNode(claim);

                if (claims.TryGetValue(claim.Type, out var existingValue))
                {
                    claims[claim.Type] = MergeClaimValues(existingValue, currentValue);
                }
                else
                {
                    claims[claim.Type] = currentValue;
                }
            }

            return Task.FromResult<TokenInfo?>(new TokenInfo
            {
                Subject = jwtToken.Subject,
                Issuer = jwtToken.Issuer,
                Audience = jwtToken.Audiences.FirstOrDefault(),
                IssuedAt = issuedAt,
                Expires = expiresAt,
                Claims = claims
            });
        }
        catch
        {
            return Task.FromResult<TokenInfo?>(null);
        }
    }

    private static DateTime ExtractLifetime(JwtSecurityToken token, string claimType, DateTime fallback)
    {
        var normalizedFallback = NormalizeToUtc(fallback);
        if (normalizedFallback != default)
        {
            return normalizedFallback;
        }

        if (token.Payload.TryGetValue(claimType, out var value) && TryConvertToUtcDateTime(value, out var parsed))
        {
            return parsed;
        }

        return default;
    }

    private static bool TryConvertToUtcDateTime(object value, out DateTime result)
    {
        switch (value)
        {
            case long longValue:
                result = DateTimeOffset.FromUnixTimeSeconds(longValue).UtcDateTime;
                return true;
            case int intValue:
                result = DateTimeOffset.FromUnixTimeSeconds(intValue).UtcDateTime;
                return true;
            case string stringValue when long.TryParse(stringValue, out var parsedLong):
                result = DateTimeOffset.FromUnixTimeSeconds(parsedLong).UtcDateTime;
                return true;
            case DateTimeOffset dto:
                result = dto.UtcDateTime;
                return true;
            case DateTime dateTimeValue:
            {
                var normalized = NormalizeToUtc(dateTimeValue);
                if (normalized != default)
                {
                    result = normalized;
                    return true;
                }

                result = default;
                return false;
            }
            default:
                result = default;
                return false;
        }
    }

    private static JsonNode? MergeClaimValues(JsonNode? existing, JsonNode? additional)
    {
        if (additional is null)
        {
            return existing;
        }

        if (existing is null)
        {
            return additional;
        }

        if (existing is JsonArray existingArray)
        {
            existingArray.Add(CloneNode(additional));
            return existingArray;
        }

        var array = new JsonArray
        {
            CloneNode(existing),
            CloneNode(additional)
        };

        return array;
    }

    private static JsonNode? ConvertToJsonNode(Claim claim)
    {
        var value = claim.Value;

        if (string.IsNullOrEmpty(value))
        {
            return JsonValue.Create(value);
        }

        if (IsNumericClaim(claim, out var numericNode))
        {
            return numericNode;
        }

        if (IsBooleanClaim(claim, out var booleanNode))
        {
            return booleanNode;
        }

        try
        {
            return JsonNode.Parse(value);
        }
        catch (JsonException)
        {
            return JsonValue.Create(value);
        }
    }

    private static JsonNode? CloneNode(JsonNode? node) => node?.DeepClone();

    private static bool IsNumericClaim(Claim claim, out JsonNode? node)
    {
        node = null;

        switch (claim.ValueType)
        {
            case ClaimValueTypes.Integer64:
            case ClaimValueTypes.Integer32:
            case ClaimValueTypes.Integer:
            case ClaimValueTypes.UInteger64:
            case ClaimValueTypes.UInteger32:
                if (long.TryParse(claim.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longValue))
                {
                    node = JsonValue.Create(longValue);
                    return true;
                }
                break;
            case ClaimValueTypes.Double:
                if (double.TryParse(claim.Value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var doubleValue))
                {
                    node = JsonValue.Create(doubleValue);
                    return true;
                }
                break;
        }

        return false;
    }

    private static bool IsBooleanClaim(Claim claim, out JsonNode? node)
    {
        node = null;

        if (string.Equals(claim.ValueType, ClaimValueTypes.Boolean, StringComparison.Ordinal) && bool.TryParse(claim.Value, out var booleanValue))
        {
            node = JsonValue.Create(booleanValue);
            return true;
        }

        return false;
    }
}

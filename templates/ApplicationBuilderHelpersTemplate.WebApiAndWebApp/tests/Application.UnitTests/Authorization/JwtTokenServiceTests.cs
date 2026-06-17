using Infrastructure.Server.Identity.Models;
using Infrastructure.Server.Identity.Services;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using TokenClaimTypes = Domain.Identity.Constants.TokenClaimTypes;

namespace Application.UnitTests.Authorization;

public class JwtTokenServiceTests
{
	[Fact]
	public async Task GenerateToken_IncludesNormalizedScopesAndCustomClaims()
	{
		var service = CreateService();
		var additionalClaims = new[]
		{
			new Claim("tenant", "alpha"),
			new Claim(TokenClaimTypes.Subject, "ignored") // Reserved claim types are skipped
		};

		var token = await service.GenerateToken(
			userId: "user-1",
			username: "user@example.com",
			scopes: ["  api.read ", "api.read", "api.write"],
			additionalClaims: additionalClaims,
			cancellationToken: CancellationToken.None);

		var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
		var scopeValues = jwt.Claims.Where(static claim => claim.Type == "scope").Select(static claim => claim.Value).ToArray();

		Assert.Equal(["api.read", "api.write"], scopeValues);
		Assert.Contains(jwt.Claims, claim => claim.Type == "tenant" && claim.Value == "alpha");

		// JWT tokens use short claim types from Domain.Identity.Constants.ClaimTypes
		var subjectClaims = jwt.Claims.Where(static claim => claim.Type == TokenClaimTypes.Subject).ToArray();
		Assert.Single(subjectClaims);
		Assert.Equal("user-1", subjectClaims[0].Value);
		Assert.Contains(jwt.Claims, claim => claim.Type == "rbac_version" && claim.Value == "2");
	}

	[Fact]
	public async Task GenerateToken_ThrowsWhenExpirationNotInFuture()
	{
		var service = CreateService();
		var pastExpiration = DateTimeOffset.UtcNow.AddMinutes(-5);

		await Assert.ThrowsAsync<SecurityTokenException>(() => service.GenerateToken(
			userId: "user-1",
			username: "user@example.com",
			expiration: pastExpiration,
			cancellationToken: CancellationToken.None));
	}

	[Fact]
	public async Task MutateToken_AddsAndRemovesScopesAndClaims()
	{
		var service = CreateService();
		var originalToken = await service.GenerateToken(
			userId: "user-2",
			username: "user@example.com",
			scopes: ["perm.alpha", "perm.beta"],
			additionalClaims: [new Claim("tenant", "alpha")],
			cancellationToken: CancellationToken.None);

		var mutatedToken = await service.MutateToken(
			originalToken,
			scopesToAdd: ["perm.gamma"],
			scopesToRemove: ["perm.alpha"],
			claimsToAdd: [new Claim("tenant", "bravo"), new Claim("region", "eu")],
			claimsToRemove: [new Claim("tenant", "alpha")],
			cancellationToken: CancellationToken.None);

		var principal = await service.ValidateToken(mutatedToken, expectedType: null, CancellationToken.None);
		Assert.NotNull(principal);

		var scopeValues = principal!.Claims.Where(static claim => claim.Type == "scope").Select(static claim => claim.Value).ToArray();
		Assert.Equal(["perm.beta", "perm.gamma"], scopeValues);

		var tenantClaims = principal.Claims.Where(static claim => claim.Type == "tenant").Select(static claim => claim.Value).ToArray();
		Assert.Equal(["bravo"], tenantClaims);
		Assert.Contains(principal.Claims, claim => claim.Type == "region" && claim.Value == "eu");
	}

	private static JwtTokenService CreateService(JwtConfiguration? configuration = null)
	{
		configuration ??= new JwtConfiguration
		{
			Secret = "super-secret-key-value-which-is-long-enough",
			Issuer = "https://issuer.tests",
			Audience = "https://audience.tests",
			DefaultExpiration = TimeSpan.FromHours(1),
			ClockSkew = TimeSpan.FromMinutes(5)
		};

		var lazyFactory = new Lazy<Func<CancellationToken, Task<JwtConfiguration>>>(() => _ => Task.FromResult(configuration));
		return new JwtTokenService(lazyFactory);
	}
}

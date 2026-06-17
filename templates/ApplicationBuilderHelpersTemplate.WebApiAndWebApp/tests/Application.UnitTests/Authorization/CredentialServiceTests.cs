using Application.Credential.Services;
using Domain.Shared.Extensions;
using Microsoft.Extensions.Configuration;
using System.Text.Json.Nodes;

namespace Application.UnitTests.Authorization;

/// <summary>
/// Unit tests for CredentialService.
/// Validates that credentials are correctly read from configuration by environment tag.
/// </summary>
public class CredentialServiceTests
{
	[Fact]
	public async Task GetCredentials_ReadsEnvironmentSpecificValues()
	{
		var configuration = BuildConfiguration(new JsonObject
		{
			["dev"] = new JsonObject
			{
				["jwt"] = new JsonObject
				{
					["secret"] = "dev-secret",
					["issuer"] = "https://issuer.dev",
					["audience"] = "https://audience.dev",
					["default_expiration_seconds"] = 7200,
					["clock_skew_seconds"] = 45
				}
			}
		});

		var service = new CredentialService(appEnvironmentService: null!, configuration);

		var credentials = await service.GetCredentials("dev", CancellationToken.None);

		Assert.Equal("dev-secret", credentials.EnvironmentCredentials.GetValueOrThrow<string>("jwt", "secret"));
		Assert.Equal("https://issuer.dev", credentials.EnvironmentCredentials.GetValueOrThrow<string>("jwt", "issuer"));
		Assert.Equal("https://audience.dev", credentials.EnvironmentCredentials.GetValueOrThrow<string>("jwt", "audience"));
		Assert.Equal(7200, credentials.EnvironmentCredentials.GetValueOrThrow<double>("jwt", "default_expiration_seconds"));
		Assert.Equal(45, credentials.EnvironmentCredentials.GetValueOrThrow<double>("jwt", "clock_skew_seconds"));
	}

	[Fact]
	public async Task GetCredentials_ReturnsNullForMissingOptionalValues()
	{
		var configuration = BuildConfiguration(new JsonObject
		{
			["prod"] = new JsonObject
			{
				["jwt"] = new JsonObject
				{
					["secret"] = "prod-secret",
					["issuer"] = "https://issuer.prod",
					["audience"] = "https://audience.prod"
				}
			}
		});

		var service = new CredentialService(appEnvironmentService: null!, configuration);

		var credentials = await service.GetCredentials("prod", CancellationToken.None);

		Assert.Equal("prod-secret", credentials.EnvironmentCredentials.GetValueOrThrow<string>("jwt", "secret"));
		Assert.Null(credentials.EnvironmentCredentials.GetValueOrDefault<double?>(null, "jwt", "default_expiration_seconds"));
		Assert.Null(credentials.EnvironmentCredentials.GetValueOrDefault<double?>(null, "jwt", "clock_skew_seconds"));
	}

	[Fact]
	public async Task GetCredentials_ReadsNegativeValues()
	{
		var configuration = BuildConfiguration(new JsonObject
		{
			["test"] = new JsonObject
			{
				["jwt"] = new JsonObject
				{
					["secret"] = "test-secret",
					["issuer"] = "https://issuer.test",
					["audience"] = "https://audience.test",
					["default_expiration_seconds"] = -30,
					["clock_skew_seconds"] = -10
				}
			}
		});

		var service = new CredentialService(appEnvironmentService: null!, configuration);

		var credentials = await service.GetCredentials("test", CancellationToken.None);

		Assert.Equal(-30, credentials.EnvironmentCredentials.GetValueOrThrow<double>("jwt", "default_expiration_seconds"));
		Assert.Equal(-10, credentials.EnvironmentCredentials.GetValueOrThrow<double>("jwt", "clock_skew_seconds"));
	}

	[Fact]
	public async Task GetCredentials_ReadsNonJwtCredentials()
	{
		var configuration = BuildConfiguration(new JsonObject
		{
			["staging"] = new JsonObject
			{
				["database"] = new JsonObject
				{
					["connection_string"] = "Server=localhost;Database=test"
				},
				["api_keys"] = new JsonObject
				{
					["service_a"] = "key-123"
				}
			}
		});

		var service = new CredentialService(appEnvironmentService: null!, configuration);

		var credentials = await service.GetCredentials("staging", CancellationToken.None);

		Assert.Equal("Server=localhost;Database=test", credentials.EnvironmentCredentials.GetValueOrThrow<string>("database", "connection_string"));
		Assert.Equal("key-123", credentials.EnvironmentCredentials.GetValueOrThrow<string>("api_keys", "service_a"));
	}

	private static IConfiguration BuildConfiguration(JsonObject credentials)
	{
		var builder = new ConfigurationBuilder();
		builder.AddInMemoryCollection(new Dictionary<string, string?>
		{
			["VEG_RUNTIME_CREDENTIALS"] = credentials.ToJsonString()
		});
		return builder.Build();
	}
}

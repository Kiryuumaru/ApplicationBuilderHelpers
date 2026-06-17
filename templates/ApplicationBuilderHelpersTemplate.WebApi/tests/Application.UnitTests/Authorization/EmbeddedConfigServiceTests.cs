using Application.EmbeddedConfig.Services;
using Domain.Shared.Extensions;
using Microsoft.Extensions.Configuration;
using System.Text.Json.Nodes;

namespace Application.UnitTests.Authorization;

public sealed class EmbeddedConfigServiceTests
{
	[Fact]
	public async Task GetConfig_ReadsEnvironmentSpecificValues()
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

		var service = new EmbeddedConfigService(appEnvironmentService: null!, configuration);

		var config = await service.GetConfig("dev", CancellationToken.None);

		Assert.Equal("dev-secret", config.EnvironmentConfig.GetValueOrThrow<string>("jwt", "secret"));
		Assert.Equal("https://issuer.dev", config.EnvironmentConfig.GetValueOrThrow<string>("jwt", "issuer"));
		Assert.Equal("https://audience.dev", config.EnvironmentConfig.GetValueOrThrow<string>("jwt", "audience"));
		Assert.Equal(7200, config.EnvironmentConfig.GetValueOrThrow<double>("jwt", "default_expiration_seconds"));
		Assert.Equal(45, config.EnvironmentConfig.GetValueOrThrow<double>("jwt", "clock_skew_seconds"));
	}

	[Fact]
	public async Task GetConfig_ReturnsNullForMissingOptionalValues()
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

		var service = new EmbeddedConfigService(appEnvironmentService: null!, configuration);

		var config = await service.GetConfig("prod", CancellationToken.None);

		Assert.Equal("prod-secret", config.EnvironmentConfig.GetValueOrThrow<string>("jwt", "secret"));
		Assert.Null(config.EnvironmentConfig.GetValueOrDefault<double?>(null, "jwt", "default_expiration_seconds"));
		Assert.Null(config.EnvironmentConfig.GetValueOrDefault<double?>(null, "jwt", "clock_skew_seconds"));
	}

	[Fact]
	public async Task GetConfig_ReadsNegativeValues()
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

		var service = new EmbeddedConfigService(appEnvironmentService: null!, configuration);

		var config = await service.GetConfig("test", CancellationToken.None);

		Assert.Equal(-30, config.EnvironmentConfig.GetValueOrThrow<double>("jwt", "default_expiration_seconds"));
		Assert.Equal(-10, config.EnvironmentConfig.GetValueOrThrow<double>("jwt", "clock_skew_seconds"));
	}

	[Fact]
	public async Task GetConfig_ReadsNonJwtConfig()
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

		var service = new EmbeddedConfigService(appEnvironmentService: null!, configuration);

		var config = await service.GetConfig("staging", CancellationToken.None);

		Assert.Equal("Server=localhost;Database=test", config.EnvironmentConfig.GetValueOrThrow<string>("database", "connection_string"));
		Assert.Equal("key-123", config.EnvironmentConfig.GetValueOrThrow<string>("api_keys", "service_a"));
	}

	private static IConfiguration BuildConfiguration(JsonObject embeddedConfig)
	{
		var builder = new ConfigurationBuilder();
		builder.AddInMemoryCollection(new Dictionary<string, string?>
		{
			["VEG_RUNTIME_EMBEDDED_CONFIG"] = embeddedConfig.ToJsonString()
		});
		return builder.Build();
	}
}

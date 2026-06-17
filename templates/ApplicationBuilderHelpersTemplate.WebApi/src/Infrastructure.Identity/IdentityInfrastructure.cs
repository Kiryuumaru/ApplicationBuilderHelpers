using Application.Authorization.Services;
using Application.EmbeddedConfig.Interfaces.Inbound;
using ApplicationBuilderHelpers;
using Domain.Identity.Constants;
using Domain.Shared.Extensions;
using Infrastructure.Identity.Extensions;
using Infrastructure.Identity.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Identity;

public sealed class IdentityInfrastructure : ApplicationDependency
{
    public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
    {
        base.AddServices(applicationBuilder, services);

        services.AddIdentityCoreServices();

        // Register JWT token services using EmbeddedConfigService
        services.AddJwtTokenServices(async (sp, ct) =>
        {
            var embeddedConfigService = sp.GetRequiredService<IEmbeddedConfigService>();
            var config = await embeddedConfigService.GetConfig(ct);
            var jwtSecret = config.EnvironmentConfig.GetValueOrThrow<string>("jwt", "secret");
            var jwtIssuer = config.EnvironmentConfig.GetValueOrThrow<string>("jwt", "issuer");
            var jwtAudience = config.EnvironmentConfig.GetValueOrThrow<string>("jwt", "audience");
            var defaultExpirationSeconds = config.EnvironmentConfig.GetValueOrDefault<double?>(null, "jwt", "default_expiration_seconds");
            var defaultClockSkewSeconds = config.EnvironmentConfig.GetValueOrDefault<double?>(null, "jwt", "clock_skew_seconds");

            TimeSpan defaultExpiration = TokenExpirations.AccessToken;
            if (defaultExpirationSeconds.HasValue)
            {
                var expirationSeconds = Math.Max(0, defaultExpirationSeconds.Value);
                defaultExpiration = TimeSpan.FromSeconds(expirationSeconds);
            }

            TimeSpan clockSkew = JwtConfiguration.DefaultClockSkew;
            if (defaultClockSkewSeconds.HasValue)
            {
                var clockSkewSeconds = Math.Max(0, defaultClockSkewSeconds.Value);
                clockSkew = TimeSpan.FromSeconds(clockSkewSeconds);
            }
            return new JwtConfiguration()
            {
                Secret = jwtSecret,
                Issuer = jwtIssuer,
                Audience = jwtAudience,
                ClockSkew = clockSkew,
                DefaultExpiration = defaultExpiration,
            };
        });

        // Configure JWT Bearer authentication
        services.AddJwtBearerConfiguration();
    }
}

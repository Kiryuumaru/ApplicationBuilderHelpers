using Application.Credential.Interfaces.Inbound;
using Application.Credential.Models;
using Application.Credential.Services;
using Application.Server.Authorization.Services;
using ApplicationBuilderHelpers;
using Domain.Shared.Extensions;
using Infrastructure.Server.Identity.Extensions;
using Infrastructure.Server.Identity.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Server.Identity;

public class IdentityInfrastructure : ApplicationDependency
{
    public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
    {
        base.AddServices(applicationBuilder, services);

        services.AddIdentityCoreServices();

        // Register JWT token services using CredentialsService
        services.AddJwtTokenServices(async (sp, ct) =>
        {
            var credentialsService = sp.GetRequiredService<ICredentialService>();
            var credentials = await credentialsService.GetCredentials(ct);
            var jwtSecret = credentials.EnvironmentCredentials.GetValueOrThrow<string>("jwt", "secret");
            var jwtIssuer = credentials.EnvironmentCredentials.GetValueOrThrow<string>("jwt", "issuer");
            var jwtAudience = credentials.EnvironmentCredentials.GetValueOrThrow<string>("jwt", "audience");
            var defaultExpirationSeconds = credentials.EnvironmentCredentials.GetValueOrDefault<double?>(null, "jwt", "default_expiration_seconds");
            var defaultClockSkewSeconds = credentials.EnvironmentCredentials.GetValueOrDefault<double?>(null, "jwt", "clock_skew_seconds");

            TimeSpan defaultExpiration = TimeSpan.FromHours(1);
            if (defaultExpirationSeconds.HasValue)
            {
                var expirationSeconds = Math.Max(0, defaultExpirationSeconds.Value);
                defaultExpiration = TimeSpan.FromSeconds(expirationSeconds);
            }

            TimeSpan clockSkew = TimeSpan.FromMinutes(5);
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

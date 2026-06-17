using Application.Shared.Interfaces.Inbound;
using Application.Shared.Interfaces.Outbound;
using ApplicationBuilderHelpers;
using Fido2NetLib;
using Infrastructure.Server.Passkeys.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Server.Passkeys;

public class PasskeysInfrastructure : ApplicationDependency
{
    public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
    {
        base.AddServices(applicationBuilder, services);

        // Get configuration
        var configuration = applicationBuilder.Configuration;

        // Configure Fido2 from configuration or use defaults
        services.AddPasskeyInfrastructure(sp =>
        {
            var applicationConstants = sp.GetRequiredService<IApplicationConstants>();
            var fido2Section = configuration.GetSection("Fido2");

            var config = new Fido2Configuration
            {
                ServerName = fido2Section["ServerName"] ?? applicationConstants.AppName,
                ServerDomain = fido2Section["ServerDomain"] ?? "localhost"
            };

            var originsSection = fido2Section.GetSection("Origins");
            if (originsSection.Exists())
            {
                var origins = originsSection.GetChildren().Select(c => c.Value).Where(v => v is not null).Cast<string>();
                config.Origins = origins.Any() ? new HashSet<string>(origins) : new HashSet<string> { "https://localhost" };
            }
            else
            {
                config.Origins = new HashSet<string> { "https://localhost" };
            }

            return config;
        });
    }
}

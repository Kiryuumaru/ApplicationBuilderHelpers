using Application.Shared.Interfaces.Inbound;
using ApplicationBuilderHelpers;
using Fido2NetLib;
using Infrastructure.Passkeys.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Passkeys;

public sealed class PasskeysInfrastructure : ApplicationDependency
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
                config.Origins = originsSection.Get<HashSet<string>>() ?? new HashSet<string> { "https://localhost" };
            }
            else
            {
                config.Origins = new HashSet<string> { "https://localhost" };
            }

            return config;
        });
    }
}

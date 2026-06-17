using Application.Server.Identity.Interfaces.Inbound;
using Fido2NetLib;
using Infrastructure.Server.Passkeys.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Server.Passkeys.Extensions;

internal static class PasskeyServiceCollectionExtensions
{
    public static IServiceCollection AddPasskeyInfrastructure(
        this IServiceCollection services,
        Fido2Configuration config)
    {
        // Add Fido2 library
        services.AddSingleton(config);
        services.AddSingleton<IFido2, Fido2>();

        // Add passkey service
        services.AddScoped<IPasskeyService, PasskeyService>();

        return services;
    }

    public static IServiceCollection AddPasskeyInfrastructure(
        this IServiceCollection services,
        Func<IServiceProvider, Fido2Configuration> configFactory)
    {
        // Add Fido2 library with factory
        services.AddSingleton(configFactory);
        services.AddSingleton<IFido2, Fido2>();

        // Add passkey service
        services.AddScoped<IPasskeyService, PasskeyService>();

        return services;
    }
}

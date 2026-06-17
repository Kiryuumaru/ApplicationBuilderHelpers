using System.Diagnostics.CodeAnalysis;
using Application.Client.Identity.Interfaces.Inbound;
using Application.Client.Identity.Services;
using Application.Client.Shared.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Client.Identity.Extensions;

internal static class IdentityServiceCollectionExtensions
{
    internal static IServiceCollection AddIdentityServices(this IServiceCollection services)
    {
        // Token storage using local store (IndexedDB in browser)
        services.AddScoped<ITokenStorage, LocalStoreTokenStorage>();

        // Auth state notifier (singleton to share state across scopes)
        services.AddSingleton<AuthStateNotifier>();

        // Auth state provider - registered as both concrete and interface
        // Concrete type used by RunPreparationAsync for initialization
        services.AddScoped<ClientAuthStateProvider>();
        services.AddScoped<IAuthStateProvider>(sp => sp.GetRequiredService<ClientAuthStateProvider>());

        // Token refresh handler for authenticated requests
        services.AddTransient<TokenRefreshHandler>();

        // Authentication client (no token needed for login/register)
        services.AddApiClient<IAuthenticationClient, AuthenticationClient>(authenticated: false);

        // Authenticated API clients
        services.AddApiClient<ISessionsClient, SessionsClient>();
        services.AddApiClient<IApiKeysClient, ApiKeysClient>();
        services.AddApiClient<ITwoFactorClient, TwoFactorClient>();
        services.AddApiClient<IUserProfileClient, UserProfileClient>();
        services.AddApiClient<IPasskeysClient, PasskeysClient>();
        services.AddApiClient<IUsersClient, UsersClient>();

        return services;
    }

    /// <summary>
    /// Registers a typed HTTP client for API calls.
    /// </summary>
    /// <param name="authenticated">If true, adds TokenRefreshHandler for authentication.</param>
    public static IServiceCollection AddApiClient<TInterface, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>(
        this IServiceCollection services,
        bool authenticated = true)
        where TInterface : class
        where TImplementation : class, TInterface
    {
        var builder = services.AddHttpClient<TInterface, TImplementation>((sp, client) =>
        {
            client.BaseAddress = sp.GetRequiredService<IConfiguration>().GetApiEndpoint();
        });

        if (authenticated)
        {
            builder.AddHttpMessageHandler<TokenRefreshHandler>();
        }

        return services;
    }
}

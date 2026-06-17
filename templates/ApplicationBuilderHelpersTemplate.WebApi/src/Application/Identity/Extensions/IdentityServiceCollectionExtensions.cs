using Application.Authorization.Extensions;
using Application.Identity.Interfaces.Inbound;
using Application.Identity.Models;
using Application.Identity.Services;
using Application.Identity.Workers;
using Domain.Identity.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Identity.Extensions;

internal static class IdentityServiceCollectionExtensions
{
    public static IServiceCollection AddIdentityServices(this IServiceCollection services, Action<FrontendUrlOptions>? configureFrontendUrl = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Configure FrontendUrlOptions
        if (configureFrontendUrl is not null)
        {
            services.Configure(configureFrontendUrl);
        }
        else
        {
            // Default configuration - applications should override via configuration
            services.Configure<FrontendUrlOptions>(options => { });
        }

        services.AddScoped<UserAuthenticationService>();
        
        // Focused identity services
        services.AddScoped<IUserRegistrationService, UserRegistrationService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<ITwoFactorService, TwoFactorService>();
        services.AddScoped<IUserProfileService, UserProfileService>();
        services.AddScoped<IUserAuthorizationService, UserAuthorizationService>();
        services.AddScoped<ISessionService, SessionService>();
        services.AddScoped<IUserTokenService, UserTokenService>();
        services.AddScoped<IFrontendUrlBuilder, FrontendUrlBuilder>();
        services.AddScoped<IAuthMethodGuardService, AuthMethodGuardService>();
        services.AddScoped<IApiKeyService, ApiKeyService>();
        services.AddScoped<ITokenValidationService, TokenValidationService>();
        
        // Background workers
        services.AddHostedService<AnonymousUserCleanupWorker>();
        services.AddHostedService<ApiKeyCleanupWorker>();

        // OAuth service - using mock implementation by default
        // Replace with real implementation when configuring actual OAuth providers
        services.AddScoped<IOAuthService, MockOAuthService>();

        return services;
    }
}

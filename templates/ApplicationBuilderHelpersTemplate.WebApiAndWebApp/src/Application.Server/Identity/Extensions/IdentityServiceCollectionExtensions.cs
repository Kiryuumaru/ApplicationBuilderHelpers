using Application.Server.Authorization.Extensions;
using Application.Server.Identity.Interfaces.Inbound;
using Application.Server.Identity.Models;
using Application.Server.Identity.Services;
using Application.Server.Identity.Workers;
using Domain.Authorization.Models;
using Domain.Identity.Entities;
using Domain.Identity.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Server.Identity.Extensions;

internal static class IdentityServiceCollectionExtensions
{
    internal static IServiceCollection AddIdentityServices(this IServiceCollection services, Action<FrontendUrlOptions>? configureFrontendUrl = null)
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
        
        services.AddScoped<IAnonymousUserCleanupService, AnonymousUserCleanupService>();
        services.AddScoped<IApiKeyCleanupService, ApiKeyCleanupService>();

        // OAuth service - using mock implementation by default
        // Replace with real implementation when configuring actual OAuth providers
        services.AddScoped<IOAuthService, MockOAuthService>();

        // Background worker for anonymous user cleanup
        services.AddHostedService<AnonymousUserCleanupWorker>();

        // Background worker for API key cleanup
        services.AddHostedService<ApiKeyCleanupWorker>();

        return services;
    }
}

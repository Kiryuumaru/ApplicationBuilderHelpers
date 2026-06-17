using Domain.Authorization.Entities;
using Domain.Authorization.Interfaces;
using Domain.Identity.Entities;
using Domain.Identity.Interfaces;
using Infrastructure.EFCore.Identity.Configurations;
using Infrastructure.EFCore.Identity.Repositories;
using Infrastructure.EFCore.Identity.Services;
using Infrastructure.EFCore.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.EFCore.Identity.Extensions;

internal static class EFCoreIdentityServiceCollectionExtensions
{
    public static IServiceCollection AddEFCoreIdentity(this IServiceCollection services)
    {
        // Register entity configurations for modular DbContext composition
        services.AddSingleton<IEFCoreEntityConfiguration, UserEntityConfiguration>();
        services.AddSingleton<IEFCoreEntityConfiguration, RoleEntityConfiguration>();
        services.AddSingleton<IEFCoreEntityConfiguration, UserLoginEntityConfiguration>();
        services.AddSingleton<IEFCoreEntityConfiguration, UserPasskeyEntityConfiguration>();
        services.AddSingleton<IEFCoreEntityConfiguration, PasskeyChallengeEntityConfiguration>();
        services.AddSingleton<IEFCoreEntityConfiguration, PasskeyCredentialEntityConfiguration>();
        services.AddSingleton<IEFCoreEntityConfiguration, UserRoleAssignmentEntityConfiguration>();
        services.AddSingleton<IEFCoreEntityConfiguration, UserPermissionGrantEntityConfiguration>();
        services.AddSingleton<IEFCoreEntityConfiguration, LoginSessionEntityConfiguration>();
        services.AddSingleton<IEFCoreEntityConfiguration, ApiKeyEntityConfiguration>();

        // ASP.NET Core Identity stores (required for UserManager/SignInManager)
        services.AddScoped<IUserStore<User>, EFCoreAspNetUserStore>();
        services.AddScoped<IRoleStore<Role>, EFCoreAspNetRoleStore>();

        // Unit of Work implementations
        services.AddScoped<IIdentityUnitOfWork, EFCoreIdentityUnitOfWork>();
        services.AddScoped<IAuthorizationUnitOfWork, EFCoreAuthorizationUnitOfWork>();

        // Internal repositories for Application layer
        services.AddScoped<IUserRepository, EFCoreUserRepository>();
        services.AddScoped<ISessionRepository, EFCoreSessionRepository>();
        services.AddScoped<IPasskeyRepository, EFCorePasskeyRepository>();
        services.AddScoped<IRoleRepository, EFCoreRoleRepository>();
        services.AddScoped<IApiKeyRepository, EFCoreApiKeyRepository>();

        return services;
    }
}


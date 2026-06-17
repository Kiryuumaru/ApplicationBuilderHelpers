using Application.Server.Authorization.Interfaces.Outbound;
using Application.Server.Identity.Interfaces.Outbound;
using Domain.Authorization.Entities;
using Domain.Authorization.Models;
using Domain.Identity.Entities;
using Domain.Identity.Interfaces;
using Infrastructure.EFCore.Server.Identity.Configurations;
using Infrastructure.EFCore.Server.Identity.Repositories;
using Infrastructure.EFCore.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.EFCore.Server.Identity.Extensions;

internal static class EFCoreIdentityServiceCollectionExtensions
{
    public static IServiceCollection AddEFCoreIdentity(this IServiceCollection services)
    {
        // Register entity configuration for modular DbContext composition
        services.AddSingleton<IEFCoreEntityConfiguration, IdentityEntityConfiguration>();

        // ASP.NET Core Identity stores (required for UserManager/SignInManager)
        services.AddScoped<IUserStore<User>, EFCoreAspNetUserStore>();
        services.AddScoped<IRoleStore<Role>, EFCoreAspNetRoleStore>();

        // Internal repositories for Application layer
        services.AddScoped<IUserRepository, EFCoreUserRepository>();
        services.AddScoped<ISessionRepository, EFCoreSessionRepository>();
        services.AddScoped<IPasskeyRepository, EFCorePasskeyRepository>();
        services.AddScoped<IRoleRepository, EFCoreRoleRepository>();
        services.AddScoped<IApiKeyRepository, EFCoreApiKeyRepository>();

        return services;
    }
}


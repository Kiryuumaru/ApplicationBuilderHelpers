using Application.Server.Identity.Interfaces.Outbound;
using Domain.Authorization.Entities;
using Domain.Authorization.Models;
using Domain.Identity.Entities;
using Infrastructure.Server.Identity.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Infrastructure.Server.Identity.Extensions;

internal static class IdentityCoreServiceCollectionExtensions
{
    public static IServiceCollection AddIdentityCoreServices(this IServiceCollection services)
    {
        services
            .AddIdentityCore<User>()
            .AddRoles<Role>()
            .AddDefaultTokenProviders();

        // Replace the default validator to allow anonymous users without usernames.
        services.Replace(ServiceDescriptor.Scoped<IUserValidator<User>, AnonymousUserValidator>());

        // Application identity ports implemented via ASP.NET Core Identity.
        services.AddScoped<IPasswordHashService, AspNetIdentityPasswordHashService>();
        services.AddScoped<IPasswordStrengthValidator, AspNetIdentityPasswordStrengthValidator>();
        services.AddScoped<IPasswordResetTokenService, AspNetIdentityPasswordResetTokenService>();

        // Domain password verification implemented via ASP.NET Core Identity hashing.
        services.AddScoped<IPasswordVerifier, AspNetIdentityPasswordVerifier>();

        return services;
    }
}

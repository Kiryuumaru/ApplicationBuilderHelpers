using Application.Authorization.Interfaces;
using Application.Authorization.Interfaces.Inbound;
using Application.Authorization.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Authorization.Extensions;

internal static class AuthorizationServiceCollectionExtensions
{
    internal static IServiceCollection AddAuthorizationServices(this IServiceCollection services)
    {
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IUserRoleResolver, UserRoleResolver>();

        return services;
    }
}

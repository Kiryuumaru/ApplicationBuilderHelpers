using Application.Client.Authorization.Interfaces.Inbound;
using Application.Client.Authorization.Services;
using Application.Client.Identity.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Client.Authorization.Extensions;

internal static class AuthorizationServiceCollectionExtensions
{
    public static IServiceCollection AddAuthorizationServices(this IServiceCollection services)
    {
        // Client-side permission evaluation service
        services.AddScoped<IClientPermissionService, ClientPermissionService>();

        // API clients for IAM operations
        services.AddApiClient<IRolesClient, RolesClient>();
        services.AddApiClient<IPermissionsClient, PermissionsClient>();

        return services;
    }
}

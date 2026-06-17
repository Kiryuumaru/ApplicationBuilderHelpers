using Application.Credential.Interfaces.Inbound;
using Application.Credential.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Credential.Extensions;

internal static class CredentialServiceCollectionExtensions
{
    public static void AddCredentialServices(this IServiceCollection services)
    {
        services.AddScoped<ICredentialService, CredentialService>();
    }
}

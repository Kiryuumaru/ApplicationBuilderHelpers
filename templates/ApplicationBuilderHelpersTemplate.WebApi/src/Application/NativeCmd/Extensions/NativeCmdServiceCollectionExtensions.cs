using Application.NativeCmd.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Application.NativeCmd.Extensions;

internal static class NativeCmdServiceCollectionExtensions
{
    public static IServiceCollection AddNativeCmdServices(this IServiceCollection services)
    {
        services.AddScoped<CmdService>();

        return services;
    }
}

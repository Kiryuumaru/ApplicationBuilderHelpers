using Application.Shared.Interfaces.Outbound;
using Infrastructure.Mock.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Mock.Extensions;

internal static class MockServiceCollectionExtensions
{
    internal static IServiceCollection AddMockServices(this IServiceCollection services)
    {
        services.AddSingleton<IEmailService, MockEmailService>();

        return services;
    }
}

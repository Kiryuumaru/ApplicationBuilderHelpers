using Application.AppEnvironment.Extensions;
using Application.AppEnvironment.Interfaces.Inbound;
using Application.Shared.Interfaces.Inbound;
using Application.LocalStore.Interfaces.Inbound;
using Microsoft.Extensions.Configuration;

namespace Application.AppEnvironment.Services;

internal sealed class AppEnvironmentService(
    IConfiguration configuration,
    ILocalStoreFactory localStoreFactory,
    IApplicationConstants applicationConstants) : IAppEnvironmentService
{
    public async Task<Domain.AppEnvironment.Models.AppEnvironment> GetEnvironment(CancellationToken cancellationToken = default)
    {
        string? appTag = configuration.GetAppTagOverride();
        if (string.IsNullOrEmpty(appTag))
        {
            using var store = await localStoreFactory.OpenStoreAsync(cancellationToken: cancellationToken);
            var storedValue = await store.GetAsync("APP_TAG", cancellationToken);
            if (!string.IsNullOrWhiteSpace(storedValue))
            {
                appTag = storedValue;
            }
        }
        if (string.IsNullOrEmpty(appTag))
        {
            appTag = applicationConstants.AppTag;
        }
        return Domain.AppEnvironment.Constants.AppEnvironments.GetByTag(appTag);
    }
}

using Application.AppEnvironment.Interfaces.Inbound;
using Application.EmbeddedConfig.Extensions;
using Application.EmbeddedConfig.Interfaces.Inbound;
using Application.EmbeddedConfig.Models;
using Domain.Shared.Extensions;
using Microsoft.Extensions.Configuration;

namespace Application.EmbeddedConfig.Services;

internal sealed class EmbeddedConfigService(IAppEnvironmentService appEnvironmentService, IConfiguration configuration) : IEmbeddedConfigService
{
    public async Task<EmbeddedConfigResult> GetConfig(CancellationToken cancellationToken)
    {
        var appEnv = await appEnvironmentService.GetEnvironment(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        var sharedConfig = configuration.GetEmbeddedConfig("shared");
        var envConfig = configuration.GetEmbeddedConfig("environments", appEnv.Tag);
        return new EmbeddedConfigResult
        {
            SharedConfig = sharedConfig,
            EnvironmentConfig = envConfig,
        };
    }
}

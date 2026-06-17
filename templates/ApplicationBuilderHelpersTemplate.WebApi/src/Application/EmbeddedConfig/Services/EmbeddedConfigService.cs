using Application.AppEnvironment.Interfaces.Inbound;
using Application.EmbeddedConfig.Extensions;
using Application.EmbeddedConfig.Interfaces.Inbound;
using Application.EmbeddedConfig.Models;
using Domain.Shared.Extensions;
using Microsoft.Extensions.Configuration;

namespace Application.EmbeddedConfig.Services;

internal sealed class EmbeddedConfigService(IAppEnvironmentService appEnvironmentService, IConfiguration configuration) : IEmbeddedConfigService
{
    public async Task<EmbeddedConfigResult> GetConfig(string envTag, CancellationToken cancellationToken)
    {
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();
        var envConfig = configuration.GetEmbeddedConfig(envTag);
        return new EmbeddedConfigResult
        {
            EnvironmentConfig = envConfig,
        };
    }

    public async Task<EmbeddedConfigResult> GetConfig(CancellationToken cancellationToken)
    {
        var appEnv = await appEnvironmentService.GetEnvironment(cancellationToken);
        return await GetConfig(appEnv.Tag, cancellationToken);
    }
}

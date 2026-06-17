using Application.AppEnvironment.Interfaces.Inbound;
using Application.Credential.Extensions;
using Application.Credential.Interfaces.Inbound;
using Application.Credential.Models;
using Domain.Shared.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace Application.Credential.Services;

internal class CredentialService(IAppEnvironmentService appEnvironmentService, IConfiguration configuration) : ICredentialService
{
    public async Task<Models.Credentials> GetCredentials(string envTag, CancellationToken cancellationToken)
    {
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();
        var envCredentials = configuration.GetCredentials(envTag);
        return new Models.Credentials
        {
            EnvironmentCredentials = envCredentials,
        };
    }

    public async Task<Models.Credentials> GetCredentials(CancellationToken cancellationToken)
    {
        var appEnv = await appEnvironmentService.GetEnvironment(cancellationToken);
        return await GetCredentials(appEnv.Tag, cancellationToken);
    }
}

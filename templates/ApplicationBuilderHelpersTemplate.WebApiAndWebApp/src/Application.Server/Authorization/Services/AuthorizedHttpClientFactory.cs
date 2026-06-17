using Application.Server.Authorization.Extensions;
using Application.Server.Authorization.Interfaces.Inbound;
using Application.Logger.Extensions;
using DisposableHelpers.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Server.Authorization.Services;

[Disposable]
internal partial class AuthorizedHttpClientFactory(string serviceKey, ILogger<AuthorizedHttpClientFactory> logger, IHttpClientFactory httpClientFactory, IPermissionService permissionService) : IAuthorizedHttpClientFactory
{
    private readonly List<HttpClient> httpClients = [];

    public async Task<HttpClient> CreateAuthorizedAsync(string clientName, IEnumerable<string> permissionIdentifiers, TimeSpan expiration, CancellationToken cancellationToken = default)
    {
        using var _ = logger.BeginScopeMap();
        var httpClient = httpClientFactory.CreateClient(serviceKey);
        logger.LogInformation("Creating HTTP client '{ClientName}' for '{ExpirationHours}' hours with permissions: {Permissions}", clientName, expiration.TotalHours, string.Join(", ", permissionIdentifiers));
        var token = await permissionService.GenerateApiKeyTokenWithPermissionsAsync(
            apiKeyName: clientName, 
            permissionIdentifiers: permissionIdentifiers, 
            expiration: expiration, 
            cancellationToken: cancellationToken);
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        httpClient.Timeout = expiration;
        httpClients.Add(httpClient);
        return httpClient;
    }

    protected void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var httpClient in httpClients) 
            {
                httpClient.Dispose();
            }
        }
    }
}

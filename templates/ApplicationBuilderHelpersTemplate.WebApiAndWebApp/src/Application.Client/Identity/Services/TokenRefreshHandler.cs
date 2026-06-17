using System.Net;
using System.Net.Http.Headers;
using Application.Client.Identity.Interfaces.Inbound;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Client.Identity.Services;

internal class TokenRefreshHandler(IServiceScopeFactory scopeFactory) : DelegatingHandler
{
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Create scope to read from persistent token storage (IndexedDB)
        Models.StoredCredentials? credentials;
        using (var scope = scopeFactory.CreateScope())
        {
            var tokenStorage = scope.ServiceProvider.GetRequiredService<ITokenStorage>();
            credentials = await tokenStorage.GetCredentialsAsync();
        }
        
        if (credentials != null && !string.IsNullOrEmpty(credentials.AccessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", credentials.AccessToken);
        }

        var response = await base.SendAsync(request, cancellationToken);

        // If unauthorized and we have a refresh token, try to refresh
        if (response.StatusCode == HttpStatusCode.Unauthorized && credentials?.RefreshToken != null)
        {
            await _refreshLock.WaitAsync(cancellationToken);
            try
            {
                // Re-check credentials (another request might have refreshed)
                using var scope = scopeFactory.CreateScope();
                var tokenStorage = scope.ServiceProvider.GetRequiredService<ITokenStorage>();
                var currentCredentials = await tokenStorage.GetCredentialsAsync();
                
                if (currentCredentials?.AccessToken != credentials.AccessToken)
                {
                    // Token was already refreshed, retry with new token
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", currentCredentials!.AccessToken);
                    return await base.SendAsync(CloneRequest(request), cancellationToken);
                }

                var authClient = scope.ServiceProvider.GetRequiredService<IAuthenticationClient>();
                var authStateProvider = scope.ServiceProvider.GetRequiredService<IAuthStateProvider>();

                // Attempt token refresh
                var refreshResult = await authClient.RefreshTokenAsync(credentials.RefreshToken, cancellationToken);
                if (refreshResult.Success)
                {
                    var newCredentials = new Models.StoredCredentials
                    {
                        AccessToken = refreshResult.AccessToken!,
                        RefreshToken = refreshResult.RefreshToken!,
                        AccessTokenExpiry = DateTimeOffset.UtcNow.AddSeconds(refreshResult.ExpiresIn),
                        RefreshTokenExpiry = DateTimeOffset.UtcNow.AddDays(7), // Assume 7 day refresh token
                        Roles = refreshResult.Roles.ToList(),
                        Permissions = refreshResult.Permissions.ToList()
                    };

                    await authStateProvider.UpdateStateAsync(newCredentials);

                    // Retry the original request with new token
                    var retryRequest = CloneRequest(request);
                    retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newCredentials.AccessToken);
                    return await base.SendAsync(retryRequest, cancellationToken);
                }
                else
                {
                    // Refresh failed - clear auth state (UI will react via OnStateChanged)
                    await authStateProvider.ClearStateAsync();
                }
            }
            finally
            {
                _refreshLock.Release();
            }
        }
        // Handle 401 when no refresh token available (already logged out or session revoked elsewhere)
        else if (response.StatusCode == HttpStatusCode.Unauthorized && credentials != null)
        {
            using var scope = scopeFactory.CreateScope();
            var authStateProvider = scope.ServiceProvider.GetRequiredService<IAuthStateProvider>();
            await authStateProvider.ClearStateAsync();
        }

        return response;
    }

    private static HttpRequestMessage CloneRequest(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);
        clone.Content = request.Content;
        clone.Version = request.Version;

        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        foreach (var option in request.Options)
        {
            clone.Options.TryAdd(option.Key, option.Value);
        }

        return clone;
    }
}

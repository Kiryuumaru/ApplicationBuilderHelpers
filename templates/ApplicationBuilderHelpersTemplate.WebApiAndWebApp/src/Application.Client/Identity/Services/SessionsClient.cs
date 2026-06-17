using System.Net.Http.Json;
using Application.Client.Identity.Interfaces.Inbound;
using Application.Client.Identity.Models;
using Application.Client.Serialization;

namespace Application.Client.Identity.Services;

internal class SessionsClient(HttpClient httpClient) : ISessionsClient
{
    public async Task<List<SessionInfo>> ListSessionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.GetAsync($"api/v1/auth/users/{userId}/sessions", cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync(
                    ApplicationClientJsonContext.Default.ListResponseSessionInfo,
                    cancellationToken);
                return result?.Items ?? new List<SessionInfo>();
            }

            return new List<SessionInfo>();
        }
        catch
        {
            return new List<SessionInfo>();
        }
    }

    public async Task<bool> RevokeSessionAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.DeleteAsync($"api/v1/auth/users/{userId}/sessions/{sessionId}", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<int> RevokeAllSessionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.DeleteAsync($"api/v1/auth/users/{userId}/sessions", cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync(
                    ApplicationClientJsonContext.Default.RevokeAllResponse,
                    cancellationToken);
                return result?.RevokedCount ?? 0;
            }

            return 0;
        }
        catch
        {
            return 0;
        }
    }
}

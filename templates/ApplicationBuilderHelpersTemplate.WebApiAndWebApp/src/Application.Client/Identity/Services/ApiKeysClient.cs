using System.Net.Http.Json;
using Application.Client.Identity.Interfaces.Inbound;
using Application.Client.Identity.Models;
using Application.Client.Serialization;

namespace Application.Client.Identity.Services;

internal class ApiKeysClient(HttpClient httpClient) : IApiKeysClient
{
    public async Task<List<ApiKeyInfo>> ListApiKeysAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.GetAsync($"api/v1/auth/users/{userId}/api-keys", cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync(
                    ApplicationClientJsonContext.Default.ListResponseApiKeyInfo,
                    cancellationToken);
                return result?.Items ?? new List<ApiKeyInfo>();
            }

            return new List<ApiKeyInfo>();
        }
        catch
        {
            return new List<ApiKeyInfo>();
        }
    }

    public async Task<CreateApiKeyResult?> CreateApiKeyAsync(Guid userId, string name, DateTimeOffset? expiresAt = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new CreateApiKeyRequest
            {
                Name = name,
                ExpiresAt = expiresAt
            };

            var response = await httpClient.PostAsJsonAsync(
                $"api/v1/auth/users/{userId}/api-keys",
                request,
                ApplicationClientJsonContext.Default.CreateApiKeyRequest,
                cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync(
                    ApplicationClientJsonContext.Default.CreateApiKeyResult,
                    cancellationToken);
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> RevokeApiKeyAsync(Guid userId, Guid apiKeyId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.DeleteAsync($"api/v1/auth/users/{userId}/api-keys/{apiKeyId}", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}

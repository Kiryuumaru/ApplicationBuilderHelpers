using System.Net.Http.Json;
using Application.Client.Identity.Interfaces.Inbound;
using Application.Client.Identity.Models;
using Application.Client.Serialization;

namespace Application.Client.Identity.Services;

internal class UsersClient(HttpClient httpClient) : IUsersClient
{
    public async Task<List<IamUser>> ListUsersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.GetAsync("api/v1/iam/users", cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync(
                    ApplicationClientJsonContext.Default.PagedResponseIamUser,
                    cancellationToken);
                return result?.Items ?? new List<IamUser>();
            }

            return new List<IamUser>();
        }
        catch
        {
            return new List<IamUser>();
        }
    }

    public async Task<IamUser?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.GetAsync($"api/v1/iam/users/{userId}", cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync(
                    ApplicationClientJsonContext.Default.IamUser,
                    cancellationToken);
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<IamUser?> UpdateUserAsync(Guid userId, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.PutAsJsonAsync(
                $"api/v1/iam/users/{userId}",
                request,
                ApplicationClientJsonContext.Default.UpdateUserRequest,
                cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync(
                    ApplicationClientJsonContext.Default.IamUser,
                    cancellationToken);
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.DeleteAsync($"api/v1/iam/users/{userId}", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<UserPermissions?> GetUserPermissionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.GetAsync($"api/v1/iam/users/{userId}/permissions", cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync(
                    ApplicationClientJsonContext.Default.UserPermissions,
                    cancellationToken);
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> ResetUserPasswordAsync(Guid userId, string newPassword, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new ResetUserPasswordRequest
            {
                NewPassword = newPassword
            };

            var response = await httpClient.PutAsJsonAsync(
                $"api/v1/iam/users/{userId}/password",
                request,
                ApplicationClientJsonContext.Default.ResetUserPasswordRequest,
                cancellationToken);
            
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}

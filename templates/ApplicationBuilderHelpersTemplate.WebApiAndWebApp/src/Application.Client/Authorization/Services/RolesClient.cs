using System.Net.Http.Json;
using Application.Client.Authorization.Interfaces.Inbound;
using Application.Client.Authorization.Models;
using Application.Client.Serialization;

namespace Application.Client.Authorization.Services;

internal class RolesClient(HttpClient httpClient) : IRolesClient
{
    public async Task<List<IamRole>> ListRolesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.GetAsync("api/v1/iam/roles", cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync(
                    ApplicationClientJsonContext.Default.ListResponseIamRole,
                    cancellationToken);
                return result?.Items ?? new List<IamRole>();
            }

            return new List<IamRole>();
        }
        catch
        {
            return new List<IamRole>();
        }
    }

    public async Task<IamRole?> GetRoleAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.GetAsync($"api/v1/iam/roles/{roleId}", cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync(
                    ApplicationClientJsonContext.Default.IamRole,
                    cancellationToken);
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<IamRole?> CreateRoleAsync(CreateRoleRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync(
                "api/v1/iam/roles",
                request,
                ApplicationClientJsonContext.Default.CreateRoleRequest,
                cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync(
                    ApplicationClientJsonContext.Default.IamRole,
                    cancellationToken);
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<IamRole?> UpdateRoleAsync(Guid roleId, UpdateRoleRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.PutAsJsonAsync(
                $"api/v1/iam/roles/{roleId}",
                request,
                ApplicationClientJsonContext.Default.UpdateRoleRequest,
                cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync(
                    ApplicationClientJsonContext.Default.IamRole,
                    cancellationToken);
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> DeleteRoleAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.DeleteAsync($"api/v1/iam/roles/{roleId}", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> AssignRoleAsync(AssignRoleRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync(
                "api/v1/iam/roles/assign",
                request,
                ApplicationClientJsonContext.Default.AssignRoleRequest,
                cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UnassignRoleAsync(UnassignRoleRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync(
                "api/v1/iam/roles/remove",
                request,
                ApplicationClientJsonContext.Default.UnassignRoleRequest,
                cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}

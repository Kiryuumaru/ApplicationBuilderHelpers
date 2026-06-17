using System.Text.Json;
using Application.Client.Identity.Interfaces.Inbound;
using Application.Client.Identity.Models;
using Application.Client.Serialization;
using Application.LocalStore.Interfaces.Outbound;

namespace Application.Client.Identity.Services;

internal sealed class LocalStoreTokenStorage(ILocalStoreService localStoreService) : ITokenStorage
{
    private const string GroupName = "auth";
    private const string CredentialsId = "credentials";

    public async Task StoreCredentialsAsync(StoredCredentials credentials)
    {
        var json = JsonSerializer.Serialize(credentials, ApplicationClientJsonContext.Default.StoredCredentials);
        await localStoreService.Open(CancellationToken.None);
        await localStoreService.Set(GroupName, CredentialsId, json, CancellationToken.None);
        await localStoreService.CommitAsync(CancellationToken.None);
    }

    public async Task<StoredCredentials?> GetCredentialsAsync()
    {
        try
        {
            await localStoreService.Open(CancellationToken.None);
            var json = await localStoreService.Get(GroupName, CredentialsId, CancellationToken.None);
            
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            return JsonSerializer.Deserialize(json, ApplicationClientJsonContext.Default.StoredCredentials);
        }
        catch
        {
            return null;
        }
    }

    public async Task ClearCredentialsAsync()
    {
        await localStoreService.Open(CancellationToken.None);
        await localStoreService.Set(GroupName, CredentialsId, null, CancellationToken.None);
        await localStoreService.CommitAsync(CancellationToken.None);
    }

    public async Task<bool> HasCredentialsAsync()
    {
        try
        {
            await localStoreService.Open(CancellationToken.None);
            return await localStoreService.Contains(GroupName, CredentialsId, CancellationToken.None);
        }
        catch
        {
            return false;
        }
    }
}

using System.Net.Http.Json;
using Application.Client.Identity.Interfaces.Inbound;
using Application.Client.Identity.Models;
using Application.Client.Serialization;

namespace Application.Client.Identity.Services;

internal class TwoFactorClient(HttpClient httpClient) : ITwoFactorClient
{
    public async Task<TwoFactorSetupInfo?> GetSetupInfoAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.GetAsync($"api/v1/auth/users/{userId}/2fa/setup", cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync(
                    ApplicationClientJsonContext.Default.TwoFactorSetupInfo,
                    cancellationToken);
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<EnableTwoFactorResult> EnableAsync(Guid userId, string verificationCode, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new EnableTwoFactorRequest
            {
                VerificationCode = verificationCode
            };

            var response = await httpClient.PostAsJsonAsync(
                $"api/v1/auth/users/{userId}/2fa/enable",
                request,
                ApplicationClientJsonContext.Default.EnableTwoFactorRequest,
                cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync(
                    ApplicationClientJsonContext.Default.EnableTwoFactorResponse,
                    cancellationToken);
                return EnableTwoFactorResult.Succeeded(result?.RecoveryCodes ?? new List<string>());
            }

            var error = await response.Content.ReadFromJsonAsync(
                ApplicationClientJsonContext.Default.ErrorResponse,
                cancellationToken);
            return EnableTwoFactorResult.Failed(error?.Message ?? "Failed to enable 2FA");
        }
        catch (Exception ex)
        {
            return EnableTwoFactorResult.Failed($"Network error: {ex.Message}");
        }
    }

    public async Task<bool> DisableAsync(Guid userId, string password, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new DisableTwoFactorRequest
            {
                Password = password
            };

            var response = await httpClient.PostAsJsonAsync(
                $"api/v1/auth/users/{userId}/2fa/disable",
                request,
                ApplicationClientJsonContext.Default.DisableTwoFactorRequest,
                cancellationToken);
            
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<string>> GenerateRecoveryCodesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.PostAsync($"api/v1/auth/users/{userId}/2fa/recovery-codes", null, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync(
                    ApplicationClientJsonContext.Default.RecoveryCodesResponse,
                    cancellationToken);
                return result?.RecoveryCodes ?? new List<string>();
            }

            return new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }
}

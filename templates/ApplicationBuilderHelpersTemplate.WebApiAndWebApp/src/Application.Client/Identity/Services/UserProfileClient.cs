using System.Net.Http.Json;
using Application.Client.Identity.Interfaces.Inbound;
using Application.Client.Identity.Models;
using Application.Client.Serialization;

namespace Application.Client.Identity.Services;

internal class UserProfileClient(HttpClient httpClient) : IUserProfileClient
{
    public async Task<UserProfile?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.GetAsync("api/v1/auth/me", cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync(
                    ApplicationClientJsonContext.Default.UserProfile,
                    cancellationToken);
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new ChangePasswordRequest
            {
                CurrentPassword = currentPassword,
                NewPassword = newPassword
            };

            var response = await httpClient.PutAsJsonAsync(
                $"api/v1/auth/users/{userId}/identity/password",
                request,
                ApplicationClientJsonContext.Default.ChangePasswordRequest,
                cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }

            var error = await response.Content.ReadFromJsonAsync(
                ApplicationClientJsonContext.Default.ErrorResponse,
                cancellationToken);
            return (false, error?.Detail ?? error?.Message ?? "Failed to change password");
        }
        catch (Exception ex)
        {
            return (false, $"Network error: {ex.Message}");
        }
    }

    public async Task<(UserProfile? Profile, string? ErrorMessage)> ChangeUsernameAsync(Guid userId, string newUsername, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new ChangeUsernameRequest
            {
                Username = newUsername
            };

            var response = await httpClient.PutAsJsonAsync(
                $"api/v1/auth/users/{userId}/identity/username",
                request,
                ApplicationClientJsonContext.Default.ChangeUsernameRequest,
                cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                var profile = await response.Content.ReadFromJsonAsync(
                    ApplicationClientJsonContext.Default.UserProfile,
                    cancellationToken);
                return (profile, null);
            }

            var error = await response.Content.ReadFromJsonAsync(
                ApplicationClientJsonContext.Default.ErrorResponse,
                cancellationToken);
            return (null, error?.Detail ?? error?.Message ?? "Failed to change username");
        }
        catch (Exception ex)
        {
            return (null, $"Network error: {ex.Message}");
        }
    }

    public async Task<(UserProfile? Profile, string? ErrorMessage)> ChangeEmailAsync(Guid userId, string newEmail, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new ChangeEmailRequest
            {
                Email = newEmail
            };

            var response = await httpClient.PutAsJsonAsync(
                $"api/v1/auth/users/{userId}/identity/email",
                request,
                ApplicationClientJsonContext.Default.ChangeEmailRequest,
                cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                var profile = await response.Content.ReadFromJsonAsync(
                    ApplicationClientJsonContext.Default.UserProfile,
                    cancellationToken);
                return (profile, null);
            }

            var error = await response.Content.ReadFromJsonAsync(
                ApplicationClientJsonContext.Default.ErrorResponse,
                cancellationToken);
            return (null, error?.Detail ?? error?.Message ?? "Failed to change email");
        }
        catch (Exception ex)
        {
            return (null, $"Network error: {ex.Message}");
        }
    }
}

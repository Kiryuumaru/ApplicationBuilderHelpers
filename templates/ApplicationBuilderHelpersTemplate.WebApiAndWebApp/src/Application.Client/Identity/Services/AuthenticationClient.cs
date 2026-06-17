using System.Net.Http.Headers;
using System.Net.Http.Json;
using Application.Client.Identity.Interfaces.Inbound;
using Application.Client.Identity.Models;
using Application.Client.Serialization;

namespace Application.Client.Identity.Services;

internal class AuthenticationClient(HttpClient httpClient, ITokenStorage tokenStorage) : IAuthenticationClient
{
    public async Task<LoginResult> LoginAsync(string usernameOrEmail, string password, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new LoginRequest
            {
                Username = usernameOrEmail,
                Password = password
            };

            var response = await httpClient.PostAsJsonAsync(
                "api/v1/auth/login",
                request,
                ApplicationClientJsonContext.Default.LoginRequest,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync(
                    ApplicationClientJsonContext.Default.TokenResponse,
                    cancellationToken);
                if (result != null)
                {
                    return LoginResult.Succeeded(
                        result.AccessToken,
                        result.RefreshToken,
                        result.ExpiresIn,
                        result.User?.Roles,
                        result.User?.Permissions);
                }
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                var error = await response.Content.ReadFromJsonAsync(
                    ApplicationClientJsonContext.Default.ErrorResponse,
                    cancellationToken);
                if (error?.RequiresTwoFactor == true)
                {
                    return LoginResult.TwoFactorRequired(error.TwoFactorToken ?? string.Empty);
                }
                return LoginResult.Failed(error?.Message ?? "Invalid credentials");
            }

            return LoginResult.Failed("Login failed");
        }
        catch (Exception ex)
        {
            return LoginResult.Failed($"Network error: {ex.Message}");
        }
    }

    public async Task<LoginResult> RegisterAsync(string email, string username, string password, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new RegisterRequest
            {
                Email = email,
                Username = username,
                Password = password,
                ConfirmPassword = password
            };

            var response = await httpClient.PostAsJsonAsync(
                "api/v1/auth/register",
                request,
                ApplicationClientJsonContext.Default.RegisterRequest,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync(
                    ApplicationClientJsonContext.Default.TokenResponse,
                    cancellationToken);
                if (result != null)
                {
                    return LoginResult.Succeeded(
                        result.AccessToken,
                        result.RefreshToken,
                        result.ExpiresIn,
                        result.User?.Roles,
                        result.User?.Permissions);
                }
            }

            var error = await response.Content.ReadFromJsonAsync(
                ApplicationClientJsonContext.Default.ErrorResponse,
                cancellationToken);
            return LoginResult.Failed(error?.Message ?? "Registration failed");
        }
        catch (Exception ex)
        {
            return LoginResult.Failed($"Network error: {ex.Message}");
        }
    }

    public async Task<LoginResult> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new RefreshTokenRequest
            {
                RefreshToken = refreshToken
            };

            var response = await httpClient.PostAsJsonAsync(
                "api/v1/auth/refresh",
                request,
                ApplicationClientJsonContext.Default.RefreshTokenRequest,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync(
                    ApplicationClientJsonContext.Default.TokenResponse,
                    cancellationToken);
                if (result != null)
                {
                    return LoginResult.Succeeded(
                        result.AccessToken,
                        result.RefreshToken,
                        result.ExpiresIn,
                        result.User?.Roles,
                        result.User?.Permissions);
                }
            }

            return LoginResult.Failed("Token refresh failed");
        }
        catch (Exception ex)
        {
            return LoginResult.Failed($"Network error: {ex.Message}");
        }
    }

    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var credentials = await tokenStorage.GetCredentialsAsync();
            if (credentials?.AccessToken == null)
            {
                return;
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/auth/logout");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", credentials.AccessToken);
            await httpClient.SendAsync(request, cancellationToken);
        }
        catch
        {
            // Ignore logout errors - user is logging out anyway
        }
    }

    public async Task<bool> ForgotPasswordAsync(string email, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new ForgotPasswordRequest
            {
                Email = email
            };

            var response = await httpClient.PostAsJsonAsync(
                "api/v1/auth/forgot-password",
                request,
                ApplicationClientJsonContext.Default.ForgotPasswordRequest,
                cancellationToken);

            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> ResetPasswordAsync(string email, string token, string newPassword, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new ResetPasswordRequest
            {
                Email = email,
                Token = token,
                NewPassword = newPassword
            };

            var response = await httpClient.PostAsJsonAsync(
                "api/v1/auth/reset-password",
                request,
                ApplicationClientJsonContext.Default.ResetPasswordRequest,
                cancellationToken);

            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<LoginResult> ConfirmTwoFactorAsync(string code, string twoFactorToken, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new TwoFactorVerifyRequest
            {
                Code = code,
                TwoFactorToken = twoFactorToken
            };

            var response = await httpClient.PostAsJsonAsync(
                "api/v1/auth/2fa/verify",
                request,
                ApplicationClientJsonContext.Default.TwoFactorVerifyRequest,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync(
                    ApplicationClientJsonContext.Default.TokenResponse,
                    cancellationToken);
                if (result != null)
                {
                    return LoginResult.Succeeded(
                        result.AccessToken,
                        result.RefreshToken,
                        result.ExpiresIn,
                        result.User?.Roles,
                        result.User?.Permissions);
                }
            }

            return LoginResult.Failed("Two-factor verification failed");
        }
        catch (Exception ex)
        {
            return LoginResult.Failed($"Network error: {ex.Message}");
        }
    }
}

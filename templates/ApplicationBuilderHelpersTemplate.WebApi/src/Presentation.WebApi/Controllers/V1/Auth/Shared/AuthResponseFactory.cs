using Application.Shared.Utilities;
using Application.Identity.Interfaces.Inbound;
using Application.Identity.Models;
using Presentation.WebApi.Controllers.V1.Auth.Shared.Responses;

namespace Presentation.WebApi.Controllers.V1.Auth.Shared;

/// <summary>
/// Factory for creating authentication response objects.
/// Consolidates common response building logic used across Auth controllers.
/// </summary>
public sealed class AuthResponseFactory(
    IUserAuthorizationService userAuthorizationService,
    IUserTokenService userTokenService)
{
    /// <summary>
    /// Creates a <see cref="UserInfo"/> response from user authorization data.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>User information for API response.</returns>
    public async Task<UserInfo> CreateUserInfoAsync(Guid userId, CancellationToken cancellationToken)
    {
        var authData = await userAuthorizationService.GetAuthorizationDataAsync(userId, cancellationToken);

        return new UserInfo
        {
            Id = authData.UserId,
            Username = authData.Username,
            Email = authData.Email,
            Roles = authData.FormattedRoles,
            Permissions = authData.EffectivePermissions,
            IsAnonymous = authData.IsAnonymous
        };
    }

    /// <summary>
    /// Creates a new session and generates access/refresh tokens.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="userAgent">The User-Agent header value.</param>
    /// <param name="ipAddress">The client IP address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Token pair and session information.</returns>
    public async Task<(string AccessToken, string RefreshToken, Guid SessionId, int ExpiresInSeconds)> CreateSessionAndTokensAsync(
        Guid userId,
        string? userAgent,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var deviceName = DeviceInfoParser.ParseDeviceName(userAgent ?? string.Empty);
        var deviceInfo = new SessionDeviceInfo(deviceName, userAgent, ipAddress);
        var result = await userTokenService.CreateSessionWithTokensAsync(userId, deviceInfo, cancellationToken);

        return (result.AccessToken, result.RefreshToken, result.SessionId, result.ExpiresInSeconds);
    }

    /// <summary>
    /// Creates a complete <see cref="AuthResponse"/> with tokens and user info.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="userAgent">The User-Agent header value.</param>
    /// <param name="ipAddress">The client IP address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Complete authentication response.</returns>
    public async Task<AuthResponse> CreateAuthResponseAsync(
        Guid userId,
        string? userAgent,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var (accessToken, refreshToken, _, expiresIn) = await CreateSessionAndTokensAsync(
            userId, userAgent, ipAddress, cancellationToken);

        var userInfo = await CreateUserInfoAsync(userId, cancellationToken);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = expiresIn,
            User = userInfo
        };
    }
}

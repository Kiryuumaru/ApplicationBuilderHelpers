using Application.Identity.Interfaces.Inbound;
using Asp.Versioning;
using Domain.Authorization.Constants;
using Domain.Identity.Enums;
using Domain.Identity.Exceptions;
using Domain.Shared.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.WebApi.Attributes;
using Presentation.WebApi.Controllers.V1.Auth.OAuthController.Requests;
using Presentation.WebApi.Controllers.V1.Auth.OAuthController.Responses;
using Presentation.WebApi.Controllers.V1.Auth.Shared;
using Presentation.WebApi.Controllers.V1.Auth.Shared.Responses;
using Presentation.WebApi.Extensions;
using System.ComponentModel.DataAnnotations;

namespace Presentation.WebApi.Controllers.V1.Auth.OAuthController;

/// <summary>
/// Controller for OAuth and external login endpoints.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{v:apiVersion}/auth")]
[Produces("application/json")]
[Tags("Authentication")]
public sealed class AuthOAuthController(
    IOAuthService oauthService,
    IUserProfileService userProfileService,
    AuthResponseFactory authResponseFactory) : ControllerBase
{
    /// <summary>
    /// Gets available OAuth providers.
    /// </summary>
    /// <remarks>
    /// Returns all configured OAuth providers with their enabled status.
    /// Use this to display available social login options to users.
    /// Disabled providers are included but should be hidden or grayed out in the UI.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of available OAuth providers.</returns>
    /// <response code="200">Returns available providers.</response>
    [HttpGet("external/providers")]
    [AllowAnonymous]
    [ProducesResponseType<OAuthProvidersResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOAuthProviders(CancellationToken cancellationToken)
    {
        var providers = await oauthService.GetProvidersAsync(cancellationToken);

        return Ok(new OAuthProvidersResponse
        {
            Providers = providers.Select(p => new OAuthProviderResponse
            {
                Provider = p.Provider.ToString().ToLowerInvariant(),
                DisplayName = p.DisplayName,
                IsEnabled = p.IsEnabled,
                IconName = p.IconName
            }).ToList()
        });
    }

    /// <summary>
    /// Initiates OAuth login flow.
    /// </summary>
    /// <remarks>
    /// Returns an authorization URL to redirect the user to the OAuth provider.
    /// Store the returned state value to verify the callback.
    /// After user authorizes, they will be redirected to your redirect URI with a code.
    /// </remarks>
    /// <param name="provider">The OAuth provider name (e.g., "google", "github", "mock").</param>
    /// <param name="request">OAuth login request with redirect URI.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Authorization URL and state for OAuth flow.</returns>
    /// <response code="200">Returns authorization URL and state.</response>
    /// <response code="400">Provider not found or not enabled.</response>
    [HttpPost("external/{provider}")]
    [AllowAnonymous]
    [ProducesResponseType<OAuthAuthorizationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> InitiateOAuthLogin(
        string provider,
        [FromBody] OAuthLoginRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryParseProvider(provider, out var parsedProvider))
        {
            throw new Domain.Shared.Exceptions.ValidationException("provider", $"OAuth provider '{provider}' is not supported.");
        }

        var isEnabled = await oauthService.IsProviderEnabledAsync(parsedProvider, cancellationToken);
        if (!isEnabled)
        {
            throw new Domain.Shared.Exceptions.ValidationException("provider", $"OAuth provider '{provider}' is not currently enabled.");
        }

        var authUrl = await oauthService.GetAuthorizationUrlAsync(
            parsedProvider,
            request.RedirectUri,
            cancellationToken);

        return Ok(new OAuthAuthorizationResponse
        {
            AuthorizationUrl = authUrl.AuthorizationUrl,
            State = authUrl.State
        });
    }

    /// <summary>
    /// Processes OAuth callback.
    /// </summary>
    /// <remarks>
    /// Completes the OAuth flow by exchanging the authorization code for tokens.
    /// If the OAuth email matches an existing user, they are logged in.
    /// If no matching user exists, a new account is created and linked.
    /// Returns 201 Created for new users, 200 OK for existing users.
    /// </remarks>
    /// <param name="request">OAuth callback data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JWT tokens on success.</returns>
    /// <response code="200">Login successful, returns tokens.</response>
    /// <response code="201">New user registered and logged in.</response>
    /// <response code="400">Invalid callback data or state mismatch.</response>
    /// <response code="401">OAuth authentication failed.</response>
    [HttpPost("external/callback")]
    [AllowAnonymous]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ProcessOAuthCallback(
        [FromBody] OAuthCallbackRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryParseProvider(request.Provider, out var provider))
        {
            throw new Domain.Shared.Exceptions.ValidationException("provider", $"OAuth provider '{request.Provider}' is not supported.");
        }

        var result = await oauthService.ProcessLoginAsync(
            provider,
            request.Code,
            request.State,
            request.State,
            request.RedirectUri,
            cancellationToken);

        if (!result.Succeeded)
        {
            throw new OAuthAuthenticationFailedException(result.Error, result.ErrorDescription);
        }

        var userId = result.UserId ?? throw new InvalidOperationException("OAuth login succeeded but no user id was returned.");

        var userAgent = Request.Headers.UserAgent.ToString();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        var (accessToken, refreshToken, _, expiresInSeconds) = await authResponseFactory.CreateSessionAndTokensAsync(
            userId,
            userAgent,
            ipAddress,
            cancellationToken);

        var oauthUserInfo = await authResponseFactory.CreateUserInfoAsync(userId, cancellationToken);

        var response = new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = expiresInSeconds,
            User = oauthUserInfo
        };

        return this.CreatedAtMeOrOk(result.IsNewUser, response);
    }

    /// <summary>
    /// Gets the user's linked external logins.
    /// </summary>
    /// <remarks>
    /// Returns all OAuth providers linked to the user's account.
    /// Includes provider name, display name, associated email, and when it was linked.
    /// Use this to show which social accounts are connected.
    /// </remarks>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of linked external logins.</returns>
    /// <response code="200">Returns linked external logins.</response>
    /// <response code="401">User not authenticated.</response>
    [HttpGet("users/{userId:guid}/identity/external")]
    [RequiredPermission(PermissionIds.Api.Auth.Identity.External.List.Identifier)]
    [ProducesResponseType<ExternalLoginsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetExternalLogins(
        [FromRoute, Required, PermissionParameter(PermissionIds.Api.Auth.UserIdParameter)] Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await userProfileService.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            throw new EntityNotFoundException("User", userId.ToString());
        }

        return Ok(new ExternalLoginsResponse
        {
            Logins = user.ExternalLogins.Select(l => new ExternalLoginResponse
            {
                Provider = l.Provider,
                DisplayName = l.DisplayName,
                Email = l.Email,
                LinkedAt = l.LinkedAt
            }).ToList()
        });
    }

    private static bool TryParseProvider(string provider, out ExternalLoginProvider result)
    {
        return Enum.TryParse(provider, ignoreCase: true, out result);
    }
}

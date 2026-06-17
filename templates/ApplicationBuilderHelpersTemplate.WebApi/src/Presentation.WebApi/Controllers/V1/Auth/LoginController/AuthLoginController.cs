using Application.Identity.Interfaces.Inbound;
using Application.Identity.Models;
using Asp.Versioning;
using Domain.Authorization.Constants;
using Domain.Shared.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.WebApi.Attributes;
using Presentation.WebApi.Controllers.V1.Auth.LoginController.Requests;
using Presentation.WebApi.Controllers.V1.Auth.Shared;
using Presentation.WebApi.Controllers.V1.Auth.Shared.Responses;
using Presentation.WebApi.Controllers.V1.Auth.TwoFactorController.Responses;
using Presentation.WebApi.Extensions;
using TokenClaimTypes = Domain.Identity.Constants.TokenClaimTypes;

namespace Presentation.WebApi.Controllers.V1.Auth.LoginController;

/// <summary>
/// Controller for anonymous authentication endpoints (login, register) and current-session logout.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{v:apiVersion}/auth")]
[Produces("application/json")]
[Tags("Authentication")]
public sealed class AuthLoginController(
    IUserRegistrationService userRegistrationService,
    IAuthenticationService authenticationService,
    ISessionService sessionService,
    AuthResponseFactory authResponseFactory) : ControllerBase
{
    /// <summary>
    /// Authenticates a user with credentials.
    /// </summary>
    /// <remarks>
    /// Validates username/password and returns JWT tokens if successful.
    /// If two-factor authentication is enabled, returns 202 Accepted with a user ID.
    /// The client must then call the <c>/login/2fa</c> endpoint with the TOTP code.
    /// Failed attempts increment the lockout counter if lockout is enabled for the account.
    /// </remarks>
    /// <param name="request">The login credentials.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JWT access and refresh tokens along with user information, or a 2FA challenge.</returns>
    /// <response code="200">Returns the JWT tokens and user information.</response>
    /// <response code="202">Two-factor authentication is required. Returns user ID to complete 2FA.</response>
    /// <response code="401">Invalid credentials.</response>
    /// <response code="403">Account is locked.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<TwoFactorRequiredResponse>(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await authenticationService.ValidateCredentialsAsync(request.Username, request.Password, cancellationToken);

        if (!result.Succeeded && !result.RequiresTwoFactor)
        {
            throw new Domain.Identity.Exceptions.AuthenticationException("Invalid credentials.");
        }

        if (result.RequiresTwoFactor)
        {
            return Accepted(new TwoFactorRequiredResponse
            {
                UserId = result.UserId!.Value
            });
        }

        var userAgent = Request.Headers.UserAgent.ToString();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        var (accessToken, refreshToken, _, expiresIn) = await authResponseFactory.CreateSessionAndTokensAsync(
            result.UserId!.Value,
            userAgent,
            ipAddress,
            cancellationToken);

        var userInfo = await authResponseFactory.CreateUserInfoAsync(result.UserId!.Value, cancellationToken);

        return Ok(new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = expiresIn,
            User = userInfo
        });
    }

    /// <summary>
    /// Registers a new user account.
    /// </summary>
    /// <remarks>
    /// Supports two registration modes:
    /// - Anonymous: No body or empty body creates a temporary anonymous user that can be upgraded later.
    /// - Full account: Provide username and password to create a complete account.
    /// Anonymous users receive tokens but cannot login again without upgrading their account.
    /// Email is optional and can be linked after registration.
    /// </remarks>
    /// <param name="request">The registration details. All fields optional for anonymous registration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JWT tokens for the newly registered user.</returns>
    /// <response code="201">User created successfully, returns tokens.</response>
    /// <response code="400">Invalid registration data.</response>
    /// <response code="409">Username or email already exists.</response>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest? request, CancellationToken cancellationToken)
    {
        request ??= new RegisterRequest();

        if (!string.IsNullOrWhiteSpace(request.Password) && string.IsNullOrWhiteSpace(request.Username))
        {
            throw new ValidationException("Username", "Username is required when providing a password.");
        }

        if (!string.IsNullOrWhiteSpace(request.Email) && string.IsNullOrWhiteSpace(request.Username))
        {
            throw new ValidationException("Username", "Username is required when providing an email.");
        }

        if (request.IsAnonymous)
        {
            var anonymousUser = await userRegistrationService.RegisterUserAsync(null, cancellationToken);

            var userAgent = Request.Headers.UserAgent.ToString();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            var (accessToken, refreshToken, _, expiresInAnon) = await authResponseFactory.CreateSessionAndTokensAsync(
                anonymousUser.Id,
                userAgent,
                ipAddress,
                cancellationToken);

            var anonUserInfo = await authResponseFactory.CreateUserInfoAsync(anonymousUser.Id, cancellationToken);

            var response = new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = expiresInAnon,
                User = anonUserInfo
            };

            return this.CreatedAtMe(response);
        }

        var registrationRequest = new UserRegistrationRequest(
            request.Username!,
            request.Password!,
            request.ConfirmPassword,
            request.Email,
            AutoActivate: true);

        var user = await userRegistrationService.RegisterUserAsync(registrationRequest, cancellationToken);

        var regUserAgent = Request.Headers.UserAgent.ToString();
        var regIpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        var (newAccessToken, newRefreshToken, _, newExpiresIn) = await authResponseFactory.CreateSessionAndTokensAsync(
            user.Id,
            regUserAgent,
            regIpAddress,
            cancellationToken);

        var newUserInfo = await authResponseFactory.CreateUserInfoAsync(user.Id, cancellationToken);

        var newResponse = new AuthResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            ExpiresIn = newExpiresIn,
            User = newUserInfo
        };

        return this.CreatedAtMe(newResponse);
    }

    /// <summary>
    /// Logs out the current session.
    /// </summary>
    /// <remarks>
    /// Revokes the current session so the refresh token can no longer be used.
    /// Other active sessions for this user remain valid.
    /// To logout from all devices, use the <c>DELETE /users/{userId}/sessions</c> endpoint.
    /// </remarks>
    /// <param name="userId">The user ID from JWT claims (used for permission check).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success status.</returns>
    /// <response code="204">Successfully logged out.</response>
    /// <response code="401">Not authenticated.</response>
    /// <response code="403">Insufficient permissions.</response>
    [HttpPost("logout")]
    [RequiredPermission(PermissionIds.Api.Auth.Logout.Identifier)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Logout(
        [FromJwt(TokenClaimTypes.Subject), PermissionParameter(PermissionIds.Api.Auth.Logout.UserIdParameter)] Guid userId,
        CancellationToken cancellationToken)
    {
        _ = userId;

        var sessionId = User.GetSessionId();
        if (sessionId.HasValue)
        {
            await sessionService.RevokeAsync(sessionId.Value, cancellationToken);
        }
        return NoContent();
    }
}

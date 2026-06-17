using Application.Identity.Interfaces.Inbound;
using Asp.Versioning;
using Domain.Authorization.Constants;
using Domain.Identity.Exceptions;
using Domain.Shared.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.WebApi.Attributes;
using Presentation.WebApi.Controllers.V1.Auth.Shared;
using Presentation.WebApi.Controllers.V1.Auth.TwoFactorController.Requests;
using Presentation.WebApi.Controllers.V1.Auth.TwoFactorController.Responses;
using SharedResponses = Presentation.WebApi.Controllers.V1.Auth.Shared.Responses;
using System.ComponentModel.DataAnnotations;

namespace Presentation.WebApi.Controllers.V1.Auth.TwoFactorController;

/// <summary>
/// Controller for two-factor authentication (2FA) endpoints.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{v:apiVersion}/auth")]
[Produces("application/json")]
[Tags("Authentication")]
public sealed class AuthTwoFactorController(
    ITwoFactorService twoFactorService,
    IUserProfileService userProfileService,
    IAuthenticationService authenticationService,
    AuthResponseFactory authResponseFactory) : ControllerBase
{
    /// <summary>
    /// Gets the 2FA setup information.
    /// </summary>
    /// <remarks>
    /// Returns the shared key and authenticator URI for QR code generation.
    /// Display the URI as a QR code for the user to scan with their authenticator app.
    /// The formatted shared key can be entered manually if QR scanning is unavailable.
    /// </remarks>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The shared key and authenticator URI for QR code generation.</returns>
    /// <response code="200">Returns 2FA setup information.</response>
    /// <response code="401">Not authenticated.</response>
    [HttpGet("users/{userId:guid}/2fa/setup")]
    [RequiredPermission(PermissionIds.Api.Auth._2Fa.Setup.Identifier)]
    [ProducesResponseType<TwoFactorSetupResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetTwoFactorSetup(
        [FromRoute, Required, PermissionParameter(PermissionIds.Api.Auth.UserIdParameter)] Guid userId,
        CancellationToken cancellationToken)
    {
        var setupInfo = await twoFactorService.Setup2faAsync(userId, cancellationToken);

        return Ok(new TwoFactorSetupResponse
        {
            SharedKey = setupInfo.SharedKey,
            FormattedSharedKey = setupInfo.FormattedSharedKey,
            AuthenticatorUri = setupInfo.AuthenticatorUri
        });
    }

    /// <summary>
    /// Enables two-factor authentication.
    /// </summary>
    /// <remarks>
    /// Completes 2FA setup by verifying a code from the authenticator app.
    /// Returns one-time recovery codes that can be used if the authenticator is lost.
    /// Store recovery codes securely; they cannot be retrieved again.
    /// </remarks>
    /// <param name="userId">The user ID.</param>
    /// <param name="request">The verification code from the authenticator app.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Recovery codes for account recovery.</returns>
    /// <response code="200">2FA enabled successfully, returns recovery codes.</response>
    /// <response code="400">Invalid verification code.</response>
    /// <response code="401">Not authenticated.</response>
    [HttpPost("users/{userId:guid}/2fa/enable")]
    [RequiredPermission(PermissionIds.Api.Auth._2Fa.Enable.Identifier)]
    [ProducesResponseType<EnableTwoFactorResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> EnableTwoFactor(
        [FromRoute, Required, PermissionParameter(PermissionIds.Api.Auth.UserIdParameter)] Guid userId,
        [FromBody] EnableTwoFactorRequest request,
        CancellationToken cancellationToken)
    {
        var recoveryCodes = await twoFactorService.Enable2faAsync(userId, request.VerificationCode, cancellationToken);

        return Ok(new EnableTwoFactorResponse
        {
            RecoveryCodes = recoveryCodes
        });
    }

    /// <summary>
    /// Disables two-factor authentication.
    /// </summary>
    /// <remarks>
    /// Requires password confirmation to prevent unauthorized disabling.
    /// After disabling, login will no longer require a 2FA code.
    /// Previously generated recovery codes are invalidated.
    /// </remarks>
    /// <param name="userId">The user ID.</param>
    /// <param name="request">The user's password to confirm the action.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success status.</returns>
    /// <response code="204">2FA disabled successfully.</response>
    /// <response code="401">Not authenticated or invalid password.</response>
    [HttpPost("users/{userId:guid}/2fa/disable")]
    [RequiredPermission(PermissionIds.Api.Auth._2Fa.Disable.Identifier)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DisableTwoFactor(
        [FromRoute, Required, PermissionParameter(PermissionIds.Api.Auth.UserIdParameter)] Guid userId,
        [FromBody] DisableTwoFactorRequest request,
        CancellationToken cancellationToken)
    {
        var user = await userProfileService.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            throw new EntityNotFoundException("User", userId.ToString());
        }

        if (user.Username is null)
        {
            throw new Domain.Shared.Exceptions.ValidationException(
                propertyName: "Username",
                message: "The user must have a username to disable 2FA with password verification.");
        }

        var validationResult = await authenticationService.ValidateCredentialsAsync(user.Username, request.Password, cancellationToken);
        if (!validationResult.Succeeded && !validationResult.RequiresTwoFactor)
        {
            throw new InvalidPasswordException("The password is incorrect.");
        }

        await twoFactorService.Disable2faAsync(userId, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Completes login with a 2FA code.
    /// </summary>
    /// <remarks>
    /// Called after initial login returns 202 Accepted indicating 2FA is required.
    /// Accepts either a TOTP code from the authenticator app or a recovery code.
    /// Recovery codes are single-use and should be regenerated if running low.
    /// </remarks>
    /// <param name="request">The user ID and 2FA code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JWT tokens for the authenticated user.</returns>
    /// <response code="200">Returns JWT tokens.</response>
    /// <response code="401">Invalid 2FA code.</response>
    [HttpPost("login/2fa")]
    [AllowAnonymous]
    [ProducesResponseType<SharedResponses.AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> TwoFactorLogin([FromBody] TwoFactorLoginRequest request, CancellationToken cancellationToken)
    {
        var userSession = await authenticationService.Complete2faAuthenticationAsync(request.UserId, request.Code, cancellationToken);

        var userInfo = await authResponseFactory.CreateUserInfoAsync(userSession.UserId, cancellationToken);

        return Ok(new SharedResponses.AuthResponse
        {
            AccessToken = userSession.AccessToken,
            RefreshToken = userSession.RefreshToken,
            ExpiresIn = (int)(userSession.ExpiresAt - userSession.IssuedAt).TotalSeconds,
            User = userInfo
        });
    }

    /// <summary>
    /// Generates new recovery codes.
    /// </summary>
    /// <remarks>
    /// Invalidates any previously generated recovery codes and issues new ones.
    /// Returns 10 new single-use recovery codes for account recovery.
    /// Use this if you've used most recovery codes or suspect they were compromised.
    /// </remarks>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of 10 recovery codes.</returns>
    /// <response code="200">Returns the newly generated recovery codes.</response>
    /// <response code="400">2FA is not enabled on this account.</response>
    /// <response code="401">Not authenticated.</response>
    [HttpPost("users/{userId:guid}/2fa/recovery-codes")]
    [RequiredPermission(PermissionIds.Api.Auth._2Fa.RegenerateCodes.Identifier)]
    [ProducesResponseType<RecoveryCodesResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GenerateRecoveryCodes(
        [FromRoute, Required, PermissionParameter(PermissionIds.Api.Auth.UserIdParameter)] Guid userId,
        CancellationToken cancellationToken)
    {
        var recoveryCodes = await twoFactorService.GenerateRecoveryCodesAsync(userId, cancellationToken);
        return Ok(new RecoveryCodesResponse(recoveryCodes));
    }
}

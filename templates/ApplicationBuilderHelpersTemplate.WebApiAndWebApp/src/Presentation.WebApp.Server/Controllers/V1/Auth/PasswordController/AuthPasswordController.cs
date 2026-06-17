using Application.Shared.Interfaces.Inbound;
using Application.Shared.Interfaces.Outbound;
using Application.Server.Identity.Interfaces.Inbound;
using Asp.Versioning;
using Domain.Authorization.Constants;
using Domain.Identity.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.WebApp.Server.Attributes;
using Presentation.WebApp.Server.Controllers.V1.Auth.PasswordController.Requests;
using System.ComponentModel.DataAnnotations;

namespace Presentation.WebApp.Server.Controllers.V1.Auth.PasswordController;

/// <summary>
/// Controller for password-based identity operations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{v:apiVersion}/auth")]
[Produces("application/json")]
[Tags("Authentication")]
public sealed class AuthPasswordController(
    IPasswordService passwordService,
    IEmailService emailService,
    IFrontendUrlBuilder frontendUrlBuilder)
    : ControllerBase
{
    /// <summary>
    /// Changes the user's password.
    /// </summary>
    /// <remarks>
    /// Requires the current password for verification before setting a new one.
    /// The new password must meet the configured password policy (length, complexity).
    /// Existing sessions remain valid after password change.
    /// </remarks>
    /// <param name="userId">The user ID.</param>
    /// <param name="request">The password change details including current and new password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success status.</returns>
    /// <response code="204">Password changed successfully.</response>
    /// <response code="400">New password doesn't meet requirements.</response>
    /// <response code="401">Not authenticated or current password is incorrect.</response>
    [HttpPut("users/{userId:guid}/identity/password")]
    [RequiredPermission(PermissionIds.Api.Auth.Identity.Password.Change.Identifier)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword(
        [FromRoute, Required, PermissionParameter(PermissionIds.Api.Auth.UserIdParameter)] Guid userId,
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        await passwordService.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Initiates a password reset.
    /// </summary>
    /// <remarks>
    /// Sends a password reset link to the specified email address if it exists.
    /// For security, always returns success regardless of whether the email is registered.
    /// Reset tokens expire after a configured time period (typically 24 hours).
    /// </remarks>
    /// <param name="request">The email address to send reset link to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success status (always returns success for security).</returns>
    /// <response code="204">Password reset initiated (if email exists).</response>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        var token = await passwordService.GeneratePasswordResetTokenAsync(request.Email, cancellationToken);

        if (token is not null)
        {
            var resetLink = frontendUrlBuilder.BuildPasswordResetUrl(request.Email, token);
            await emailService.SendPasswordResetLinkAsync(request.Email, resetLink, cancellationToken);
        }

        return NoContent();
    }

    /// <summary>
    /// Resets the password using a reset token.
    /// </summary>
    /// <remarks>
    /// Completes the password reset flow started by <c>/forgot-password</c>.
    /// The token from the email link must be provided along with the new password.
    /// Tokens are single-use and expire after the configured time period.
    /// </remarks>
    /// <param name="request">The password reset details including token and new password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success status.</returns>
    /// <response code="204">Password reset successfully.</response>
    /// <response code="400">Invalid token or password requirements not met.</response>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var success = await passwordService.ResetPasswordWithTokenAsync(
            request.Email,
            request.Token,
            request.NewPassword,
            cancellationToken);

        if (!success)
        {
            throw new PasswordResetTokenInvalidException();
        }

        return NoContent();
    }
}

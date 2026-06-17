using Domain.Authorization.Constants;
using Domain.Shared.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Presentation.WebApi.Attributes;
using Presentation.WebApi.Controllers.V1.Auth.IdentityController.Requests;
using SharedResponses = Presentation.WebApi.Controllers.V1.Auth.Shared.Responses;
using System.ComponentModel.DataAnnotations;

namespace Presentation.WebApi.Controllers.V1.Auth.IdentityController;

public sealed partial class AuthIdentityController
{
    /// <summary>
    /// Links an email to the user's account.
    /// </summary>
    /// <remarks>
    /// Associates an email address with the account for notifications and password recovery.
    /// Email alone does not upgrade anonymous users; they need a password, OAuth, or passkey.
    /// The email must be unique across all accounts.
    /// </remarks>
    /// <param name="userId">The user ID.</param>
    /// <param name="request">The email linking request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated user information.</returns>
    /// <response code="200">Email linked successfully.</response>
    /// <response code="400">Invalid request.</response>
    /// <response code="401">Not authenticated.</response>
    /// <response code="409">Email already exists.</response>
    [HttpPost("users/{userId:guid}/identity/email")]
    [RequiredPermission(PermissionIds.Api.Auth.Identity.Email.Link.Identifier)]
    [ProducesResponseType<SharedResponses.UserInfo>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> LinkEmail(
        [FromRoute, Required, PermissionParameter(PermissionIds.Api.Auth.UserIdParameter)] Guid userId,
        [FromBody] LinkEmailRequest request,
        CancellationToken cancellationToken)
    {
        await userProfileService.LinkEmailAsync(userId, request.Email, cancellationToken);

        var linkEmailUserInfo = await authResponseFactory.CreateUserInfoAsync(
            userId,
            cancellationToken);

        return Ok(linkEmailUserInfo);
    }

    /// <summary>
    /// Changes the user's email address.
    /// </summary>
    /// <remarks>
    /// Updates the email address associated with the account.
    /// The new email must be unique and will need to be verified.
    /// Email verification status is reset after change.
    /// </remarks>
    /// <param name="userId">The user ID.</param>
    /// <param name="request">The email change request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated user information.</returns>
    /// <response code="200">Email changed successfully.</response>
    /// <response code="400">Invalid request.</response>
    /// <response code="401">Not authenticated.</response>
    /// <response code="409">Email already exists.</response>
    /// <response code="404">User not found.</response>
    [HttpPut("users/{userId:guid}/identity/email")]
    [RequiredPermission(PermissionIds.Api.Auth.Identity.Email.Change.Identifier)]
    [ProducesResponseType<SharedResponses.UserInfo>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeEmail(
        [FromRoute, Required, PermissionParameter(PermissionIds.Api.Auth.UserIdParameter)] Guid userId,
        [FromBody] ChangeEmailRequest request,
        CancellationToken cancellationToken)
    {
        var user = await userProfileService.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            throw new EntityNotFoundException("User", userId.ToString());
        }

        await userProfileService.ChangeEmailAsync(userId, request.Email, cancellationToken);

        var changeEmailUserInfo = await authResponseFactory.CreateUserInfoAsync(
            userId,
            cancellationToken);

        return Ok(changeEmailUserInfo);
    }

    /// <summary>
    /// Unlinks the email from the account.
    /// </summary>
    /// <remarks>
    /// Removes the email address from the account.
    /// Requires a username to be set since email might be needed for password recovery.
    /// After unlinking, the email can be used on another account.
    /// </remarks>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">Email unlinked successfully.</response>
    /// <response code="400">No email is linked, or email is required for login.</response>
    /// <response code="401">Not authenticated.</response>
    /// <response code="404">User not found.</response>
    [HttpDelete("users/{userId:guid}/identity/email")]
    [RequiredPermission(PermissionIds.Api.Auth.Identity.Email.Unlink.Identifier)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnlinkEmail(
        [FromRoute, Required, PermissionParameter(PermissionIds.Api.Auth.UserIdParameter)] Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await userProfileService.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            throw new EntityNotFoundException("User", userId.ToString());
        }

        if (string.IsNullOrWhiteSpace(user.Email))
        {
            throw new Domain.Shared.Exceptions.ValidationException("No email is linked to this account.");
        }

        var hasUsername = !string.IsNullOrWhiteSpace(user.Username);

        if (!hasUsername)
        {
            throw new Domain.Shared.Exceptions.ValidationException("Email is required for login because you have no username. Set a username first before unlinking email.");
        }

        await userProfileService.UnlinkEmailAsync(userId, cancellationToken);
        return NoContent();
    }
}

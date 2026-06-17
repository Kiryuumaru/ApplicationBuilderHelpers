using Application.Identity.Interfaces.Inbound;
using Domain.Authorization.Constants;
using Microsoft.AspNetCore.Mvc;
using Presentation.WebApi.Attributes;
using Presentation.WebApi.Controllers.V1.Auth.IdentityController.Requests;
using SharedResponses = Presentation.WebApi.Controllers.V1.Auth.Shared.Responses;
using System.ComponentModel.DataAnnotations;

namespace Presentation.WebApi.Controllers.V1.Auth.IdentityController;

public sealed partial class AuthIdentityController
{
    /// <summary>
    /// Links a password to the user's account.
    /// </summary>
    /// <remarks>
    /// For anonymous users, this upgrades them to a full account with username/password login.
    /// Requires a unique username and optionally an email address.
    /// The password must meet the configured password policy requirements.
    /// After linking, the user can login with username and password.
    /// </remarks>
    /// <param name="userId">The user ID.</param>
    /// <param name="request">The password linking request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated user information.</returns>
    /// <response code="200">Password linked successfully.</response>
    /// <response code="400">Invalid request or password already linked.</response>
    /// <response code="401">Not authenticated.</response>
    /// <response code="409">Username or email already exists.</response>
    [HttpPost("users/{userId:guid}/identity/password")]
    [RequiredPermission(PermissionIds.Api.Auth.Identity.Password.Link.Identifier)]
    [ProducesResponseType<SharedResponses.UserInfo>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> LinkPassword(
        [FromRoute, Required, PermissionParameter(PermissionIds.Api.Auth.UserIdParameter)] Guid userId,
        [FromBody] LinkPasswordRequest request,
        CancellationToken cancellationToken)
    {
        await passwordService.LinkPasswordAsync(
            userId,
            request.Username,
            request.Password,
            request.Email,
            cancellationToken);

        var linkPwdUserInfo = await authResponseFactory.CreateUserInfoAsync(
            userId,
            cancellationToken);

        return Ok(linkPwdUserInfo);
    }
}

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
    /// Changes the user's username.
    /// </summary>
    /// <remarks>
    /// Updates the username used for login. The new username must be unique.
    /// Anonymous users cannot change username; they must link a password or OAuth first.
    /// Existing sessions remain valid after the username change.
    /// </remarks>
    /// <param name="userId">The user ID.</param>
    /// <param name="request">The username change request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated user information.</returns>
    /// <response code="200">Username changed successfully.</response>
    /// <response code="400">Invalid request or anonymous user.</response>
    /// <response code="401">Not authenticated.</response>
    /// <response code="409">Username already exists.</response>
    /// <response code="404">User not found.</response>
    [HttpPut("users/{userId:guid}/identity/username")]
    [RequiredPermission(PermissionIds.Api.Auth.Identity.Username.Change.Identifier)]
    [ProducesResponseType<SharedResponses.UserInfo>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeUsername(
        [FromRoute, Required, PermissionParameter(PermissionIds.Api.Auth.UserIdParameter)] Guid userId,
        [FromBody] ChangeUsernameRequest request,
        CancellationToken cancellationToken)
    {
        var user = await userProfileService.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            throw new EntityNotFoundException("User", userId.ToString());
        }

        if (user.IsAnonymous)
        {
            throw new Domain.Shared.Exceptions.ValidationException("Anonymous users cannot change username. Link a password or OAuth first.");
        }

        await userProfileService.ChangeUsernameAsync(userId, request.Username, cancellationToken);

        var changeUsernameUserInfo = await authResponseFactory.CreateUserInfoAsync(
            userId,
            cancellationToken);

        return Ok(changeUsernameUserInfo);
    }
}

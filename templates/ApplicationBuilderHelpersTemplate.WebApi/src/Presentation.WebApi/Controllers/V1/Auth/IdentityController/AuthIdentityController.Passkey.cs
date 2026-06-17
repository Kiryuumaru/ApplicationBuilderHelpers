using Application.Identity.Interfaces.Inbound;
using Domain.Authorization.Constants;
using Domain.Shared.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Presentation.WebApi.Attributes;
using Presentation.WebApi.Controllers.V1.Auth.PasskeysController.Requests;
using SharedResponses = Presentation.WebApi.Controllers.V1.Auth.Shared.Responses;
using System.ComponentModel.DataAnnotations;

namespace Presentation.WebApi.Controllers.V1.Auth.IdentityController;

public sealed partial class AuthIdentityController
{
    /// <summary>
    /// Links a passkey to the user's account.
    /// </summary>
    /// <remarks>
    /// For anonymous users, this upgrades them to a full account with passwordless login.
    /// Call this after <c>navigator.credentials.create()</c> returns with the attestation response.
    /// The challenge ID must match the one from the options endpoint.
    /// Passkeys provide strong authentication without passwords.
    /// </remarks>
    /// <param name="userId">The user ID.</param>
    /// <param name="request">The passkey registration request with challenge ID and attestation response.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated user information.</returns>
    /// <response code="200">Passkey linked successfully.</response>
    /// <response code="400">Invalid attestation response or expired challenge.</response>
    /// <response code="401">Not authenticated.</response>
    /// <response code="404">User not found.</response>
    [HttpPost("users/{userId:guid}/identity/passkeys/link")]
    [RequiredPermission(PermissionIds.Api.Auth.Identity.Passkeys.Register.Identifier)]
    [ProducesResponseType<SharedResponses.UserInfo>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LinkPasskey(
        [FromRoute, Required, PermissionParameter(PermissionIds.Api.Auth.UserIdParameter)] Guid userId,
        [FromBody] PasskeyRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        var user = await userProfileService.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            throw new EntityNotFoundException("User", userId.ToString());
        }

        await passkeyService.VerifyRegistrationAsync(request.ChallengeId, request.AttestationResponseJson, cancellationToken);

        if (user.IsAnonymous)
        {
            await userRegistrationService.UpgradeAnonymousWithPasskeyAsync(userId, cancellationToken);
        }

        var linkPasskeyUserInfo = await authResponseFactory.CreateUserInfoAsync(
            userId,
            cancellationToken);

        return Ok(linkPasskeyUserInfo);
    }
}

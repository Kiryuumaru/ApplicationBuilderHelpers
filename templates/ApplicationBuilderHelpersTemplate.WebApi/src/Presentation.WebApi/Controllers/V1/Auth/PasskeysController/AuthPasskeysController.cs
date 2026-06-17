using Application.Identity.Interfaces.Inbound;
using Asp.Versioning;
using Domain.Authorization.Constants;
using Domain.Shared.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.WebApi.Attributes;
using Presentation.WebApi.Controllers.V1.Auth.PasskeysController.Requests;
using Presentation.WebApi.Controllers.V1.Auth.PasskeysController.Responses;
using Presentation.WebApi.Controllers.V1.Auth.Shared;
using Presentation.WebApi.Models.Shared;
using SharedResponses = Presentation.WebApi.Controllers.V1.Auth.Shared.Responses;
using System.ComponentModel.DataAnnotations;

namespace Presentation.WebApi.Controllers.V1.Auth.PasskeysController;

/// <summary>
/// Controller for passkey (WebAuthn) endpoints.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{v:apiVersion}/auth")]
[Produces("application/json")]
[Tags("Authentication")]
public sealed class AuthPasskeysController(
    IPasskeyService passkeyService,
    IUserProfileService userProfileService,
    IAuthMethodGuardService authMethodGuardService,
    AuthResponseFactory authResponseFactory) : ControllerBase
{
    /// <summary>
    /// Gets options for creating a new passkey.
    /// </summary>
    /// <remarks>
    /// Returns WebAuthn options required by <c>navigator.credentials.create()</c>.
    /// Pass the options JSON to the browser's WebAuthn API.
    /// The returned challenge ID must be provided when completing registration.
    /// Challenge expires after a short period (typically 5 minutes).
    /// </remarks>
    /// <param name="userId">The user ID.</param>
    /// <param name="request">The passkey registration options request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Challenge ID and WebAuthn options JSON.</returns>
    /// <response code="200">Returns the challenge ID and options for creating a passkey.</response>
    /// <response code="401">Not authenticated.</response>
    [HttpPost("users/{userId:guid}/identity/passkeys/options")]
    [RequiredPermission(PermissionIds.Api.Auth.Identity.Passkeys.Register.Identifier)]
    [ProducesResponseType<PasskeyRegistrationOptionsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetPasskeyCreationOptions(
        [FromRoute, Required, PermissionParameter(PermissionIds.Api.Auth.UserIdParameter)] Guid userId,
        [FromBody] PasskeyRegistrationOptionsRequest request,
        CancellationToken cancellationToken)
    {
        var options = await passkeyService.GetRegistrationOptionsAsync(userId, request.CredentialName, cancellationToken);

        return Ok(new PasskeyRegistrationOptionsResponse(options.ChallengeId, options.OptionsJson));
    }

    /// <summary>
    /// Registers a new passkey.
    /// </summary>
    /// <remarks>
    /// Completes passkey registration by verifying the attestation response from the browser.
    /// Call this after <c>navigator.credentials.create()</c> returns.
    /// The challenge ID must match the one from the options endpoint.
    /// Does not upgrade anonymous users; use the link endpoint for that.
    /// </remarks>
    /// <param name="userId">The user ID.</param>
    /// <param name="request">The passkey registration request with challenge ID and attestation response.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The registered passkey information.</returns>
    /// <response code="201">Passkey registered successfully.</response>
    /// <response code="400">Invalid attestation response or expired challenge.</response>
    /// <response code="401">Not authenticated.</response>
    [HttpPost("users/{userId:guid}/identity/passkeys")]
    [RequiredPermission(PermissionIds.Api.Auth.Identity.Passkeys.Register.Identifier)]
    [ProducesResponseType<PasskeyRegistrationResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RegisterPasskey(
        [FromRoute, Required, PermissionParameter(PermissionIds.Api.Auth.UserIdParameter)] Guid userId,
        [FromBody] PasskeyRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await passkeyService.VerifyRegistrationAsync(request.ChallengeId, request.AttestationResponseJson, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, new PasskeyRegistrationResponse(result.CredentialId, result.CredentialName));
    }

    /// <summary>
    /// Gets options for passkey authentication.
    /// </summary>
    /// <remarks>
    /// Returns WebAuthn options required by <c>navigator.credentials.get()</c>.
    /// Optionally provide a username to filter credentials for that user.
    /// The returned challenge ID must be provided when completing login.
    /// Challenge expires after a short period (typically 5 minutes).
    /// </remarks>
    /// <param name="request">Optional username for user-specific credential filtering.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Challenge ID and WebAuthn options JSON.</returns>
    /// <response code="200">Returns the challenge ID and options for passkey authentication.</response>
    [HttpPost("login/passkey/options")]
    [AllowAnonymous]
    [ProducesResponseType<PasskeyLoginOptionsResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPasskeyRequestOptions([FromBody] PasskeyLoginOptionsRequest? request, CancellationToken cancellationToken)
    {
        var options = await passkeyService.GetLoginOptionsAsync(request?.Username, cancellationToken);

        return Ok(new PasskeyLoginOptionsResponse(options.ChallengeId, options.OptionsJson));
    }

    /// <summary>
    /// Authenticates with a passkey.
    /// </summary>
    /// <remarks>
    /// Completes passkey login by verifying the assertion response from the browser.
    /// Call this after <c>navigator.credentials.get()</c> returns.
    /// The challenge ID must match the one from the options endpoint.
    /// Returns JWT tokens on successful authentication.
    /// </remarks>
    /// <param name="request">The passkey login request with challenge ID and assertion response.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JWT access and refresh tokens along with user information.</returns>
    /// <response code="200">Returns the JWT tokens and user information.</response>
    /// <response code="400">Invalid assertion response or expired challenge.</response>
    /// <response code="401">Authentication failed.</response>
    [HttpPost("login/passkey")]
    [AllowAnonymous]
    [ProducesResponseType<SharedResponses.AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> PasskeyLogin([FromBody] PasskeyLoginRequest request, CancellationToken cancellationToken)
    {
        var result = await passkeyService.VerifyLoginAsync(request.ChallengeId, request.AssertionResponseJson, cancellationToken);

        var userSession = result.Session;

        var passkeyUserInfo = await authResponseFactory.CreateUserInfoAsync(
            userSession.UserId,
            cancellationToken);

        return Ok(new SharedResponses.AuthResponse
        {
            AccessToken = userSession.AccessToken,
            RefreshToken = userSession.RefreshToken,
            ExpiresIn = (int)(userSession.ExpiresAt - userSession.IssuedAt).TotalSeconds,
            User = passkeyUserInfo
        });
    }

    /// <summary>
    /// Lists all passkeys for the user.
    /// </summary>
    /// <remarks>
    /// Returns all registered passkeys with their names and registration dates.
    /// Use this to display passkey management UI to users.
    /// Last used timestamp helps identify inactive credentials.
    /// </remarks>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of registered passkeys.</returns>
    /// <response code="200">Returns the list of passkeys.</response>
    /// <response code="401">Not authenticated.</response>
    [HttpGet("users/{userId:guid}/identity/passkeys")]
    [RequiredPermission(PermissionIds.Api.Auth.Identity.Passkeys.List.Identifier)]
    [ProducesResponseType<ListResponse<PasskeyInfoResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ListPasskeys(
        [FromRoute, Required, PermissionParameter(PermissionIds.Api.Auth.UserIdParameter)] Guid userId,
        CancellationToken cancellationToken)
    {
        var passkeys = await passkeyService.ListPasskeysAsync(userId, cancellationToken);

        var items = passkeys.Select(p => new PasskeyInfoResponse(p.Id, p.Name, p.RegisteredAt, p.LastUsedAt)).ToList();

        return Ok(ListResponse<PasskeyInfoResponse>.From(items));
    }

    /// <summary>
    /// Renames a passkey.
    /// </summary>
    /// <remarks>
    /// Updates the friendly name of a passkey for easier identification.
    /// The name is purely for display purposes and doesn't affect authentication.
    /// Use descriptive names like "MacBook Pro" or "iPhone 15".
    /// </remarks>
    /// <param name="userId">The user ID.</param>
    /// <param name="credentialId">The passkey credential ID.</param>
    /// <param name="request">The rename request with new name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">Passkey renamed successfully.</response>
    /// <response code="401">Not authenticated.</response>
    /// <response code="404">Passkey not found.</response>
    [HttpPut("users/{userId:guid}/identity/passkeys/{credentialId:guid}")]
    [RequiredPermission(PermissionIds.Api.Auth.Identity.Passkeys.Rename.Identifier)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RenamePasskey(
        [FromRoute, Required, PermissionParameter(PermissionIds.Api.Auth.UserIdParameter)] Guid userId,
        Guid credentialId,
        [FromBody] PasskeyRenameRequest request,
        CancellationToken cancellationToken)
    {
        await passkeyService.RenamePasskeyAsync(userId, credentialId, request.Name, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Revokes a passkey.
    /// </summary>
    /// <remarks>
    /// Permanently deletes a passkey from the account.
    /// Cannot revoke if it's the only authentication method; at least one must remain.
    /// After revocation, the passkey can no longer be used for login.
    /// </remarks>
    /// <param name="userId">The user ID.</param>
    /// <param name="credentialId">The passkey credential ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">Passkey revoked successfully.</response>
    /// <response code="400">Cannot unlink last authentication method.</response>
    /// <response code="401">Not authenticated.</response>
    /// <response code="404">Passkey not found.</response>
    [HttpDelete("users/{userId:guid}/identity/passkeys/{credentialId:guid}")]
    [RequiredPermission(PermissionIds.Api.Auth.Identity.Passkeys.Delete.Identifier)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokePasskey(
        [FromRoute, Required, PermissionParameter(PermissionIds.Api.Auth.UserIdParameter)] Guid userId,
        Guid credentialId,
        CancellationToken cancellationToken)
    {
        var user = await userProfileService.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            throw new EntityNotFoundException("User", userId.ToString());
        }

        if (!await authMethodGuardService.CanRemovePasskeyAsync(userId, credentialId, cancellationToken))
        {
            throw new Domain.Shared.Exceptions.ValidationException("You must have at least one authentication method linked to your account.");
        }

        await passkeyService.RevokePasskeyAsync(userId, credentialId, cancellationToken);
        return NoContent();
    }
}

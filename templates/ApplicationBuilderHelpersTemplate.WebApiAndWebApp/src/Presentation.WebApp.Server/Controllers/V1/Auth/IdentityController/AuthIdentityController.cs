using Application.Server.Identity.Interfaces.Inbound;
using Asp.Versioning;
using Domain.Authorization.Constants;
using Domain.Identity.Enums;
using Domain.Shared.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Presentation.WebApp.Server.Attributes;
using Presentation.WebApp.Server.Controllers.V1.Auth.IdentityController.Requests;
using Presentation.WebApp.Server.Controllers.V1.Auth.IdentityController.Responses;
using Presentation.WebApp.Server.Controllers.V1.Auth.PasskeysController.Requests;
using Presentation.WebApp.Server.Controllers.V1.Auth.Shared;
using Presentation.WebApp.Server.Controllers.V1.Auth.Shared.Responses;
using System.ComponentModel.DataAnnotations;

namespace Presentation.WebApp.Server.Controllers.V1.Auth.IdentityController;

/// <summary>
/// Controller for user identity management (password, email, username, linked providers, and passkey linking).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{v:apiVersion}/auth")]
[Produces("application/json")]
[Tags("Authentication")]
public sealed class AuthIdentityController(
    IUserRegistrationService userRegistrationService,
    IPasswordService passwordService,
    IUserProfileService userProfileService,
    IPasskeyService passkeyService,
    IAuthMethodGuardService authMethodGuardService,
    AuthResponseFactory authResponseFactory) : ControllerBase
{
    /// <summary>
    /// Gets the user's linked identities.
    /// </summary>
    /// <remarks>
    /// Returns a comprehensive view of all authentication methods linked to the account.
    /// Includes password status, email, OAuth providers, and passkeys.
    /// Use this to display account security settings and linked auth methods.
    /// </remarks>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Information about the user's linked identities.</returns>
    /// <response code="200">Returns the user's linked identities.</response>
    /// <response code="401">Not authenticated.</response>
    /// <response code="404">User not found.</response>
    [HttpGet("users/{userId:guid}/identity")]
    [RequiredPermission(PermissionIds.Api.Auth.Identity.Read.Identifier)]
    [ProducesResponseType<IdentitiesResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetIdentities(
        [FromRoute, Required, PermissionParameter(PermissionIds.Api.Auth.UserIdParameter)] Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await userProfileService.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            throw new EntityNotFoundException("User", userId.ToString());
        }

        var linkedProviders = user.ExternalLogins
            .Select(link => new LinkedProviderInfo
            {
                Provider = link.Provider,
                DisplayName = link.DisplayName,
                Email = link.Email
            })
            .ToArray();

        var passkeys = await passkeyService.ListPasskeysAsync(userId, cancellationToken);
        var linkedPasskeys = passkeys
            .Select(p => new LinkedPasskeyInfo
            {
                Id = p.Id,
                Name = p.Name,
                RegisteredAt = p.RegisteredAt
            })
            .ToArray();

        return Ok(new IdentitiesResponse
        {
            IsAnonymous = user.IsAnonymous,
            LinkedAt = user.LinkedAt,
            HasPassword = user.HasPassword,
            Email = user.Email,
            EmailConfirmed = user.EmailConfirmed,
            LinkedProviders = linkedProviders,
            LinkedPasskeys = linkedPasskeys
        });
    }

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
    [ProducesResponseType<UserInfo>(StatusCodes.Status200OK)]
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
    [ProducesResponseType<UserInfo>(StatusCodes.Status200OK)]
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
    [ProducesResponseType<UserInfo>(StatusCodes.Status200OK)]
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

    /// <summary>
    /// Unlinks an OAuth provider from the account.
    /// </summary>
    /// <remarks>
    /// Removes the association between the account and the specified OAuth provider.
    /// Cannot unlink if it's the only authentication method; at least one must remain.
    /// After unlinking, the OAuth account can be linked to a different user.
    /// </remarks>
    /// <param name="userId">The user ID.</param>
    /// <param name="provider">The provider to unlink (e.g., "google", "github").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">Provider unlinked successfully.</response>
    /// <response code="400">Cannot unlink last authentication method.</response>
    /// <response code="401">Not authenticated.</response>
    /// <response code="404">Provider not linked.</response>
    [HttpDelete("users/{userId:guid}/identity/external/{provider}")]
    [RequiredPermission(PermissionIds.Api.Auth.Identity.External.Unlink.Identifier)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnlinkProvider(
        [FromRoute, Required, PermissionParameter(PermissionIds.Api.Auth.UserIdParameter)] Guid userId,
        string provider,
        CancellationToken cancellationToken)
    {
        var user = await userProfileService.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            throw new EntityNotFoundException("User", userId.ToString());
        }

        var link = user.ExternalLogins.FirstOrDefault(l => string.Equals(l.Provider.ToString(), provider, StringComparison.OrdinalIgnoreCase));
        if (link is null)
        {
            throw new EntityNotFoundException("ExternalLogin", provider);
        }

        if (!await authMethodGuardService.CanUnlinkProviderAsync(userId, provider, cancellationToken))
        {
            throw new Domain.Shared.Exceptions.ValidationException("You must have at least one authentication method linked to your account.");
        }

        if (!Enum.TryParse<ExternalLoginProvider>(provider, ignoreCase: true, out var providerEnum))
        {
            throw new EntityNotFoundException("ExternalLoginProvider", provider);
        }

        await userProfileService.UnlinkExternalLoginAsync(userId, providerEnum, cancellationToken);
        return NoContent();
    }

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
    [ProducesResponseType<UserInfo>(StatusCodes.Status200OK)]
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
    [ProducesResponseType<UserInfo>(StatusCodes.Status200OK)]
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

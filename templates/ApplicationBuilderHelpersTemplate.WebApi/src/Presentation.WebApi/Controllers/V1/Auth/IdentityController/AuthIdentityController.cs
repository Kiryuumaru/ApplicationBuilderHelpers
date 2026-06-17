using Application.Identity.Interfaces.Inbound;
using Asp.Versioning;
using Domain.Authorization.Constants;
using Domain.Shared.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Presentation.WebApi.Attributes;
using Presentation.WebApi.Controllers.V1.Auth.IdentityController.Responses;
using Presentation.WebApi.Controllers.V1.Auth.Shared;
using System.ComponentModel.DataAnnotations;

namespace Presentation.WebApi.Controllers.V1.Auth.IdentityController;

/// <summary>
/// Controller for user identity management (password, email, username, linked providers, and passkey linking).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{v:apiVersion}/auth")]
[Produces("application/json")]
[Tags("Authentication")]
public sealed partial class AuthIdentityController(
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
}

using Domain.Authorization.Constants;
using Domain.Identity.Enums;
using Domain.Shared.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Presentation.WebApi.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Presentation.WebApi.Controllers.V1.Auth.IdentityController;

public sealed partial class AuthIdentityController
{
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
}

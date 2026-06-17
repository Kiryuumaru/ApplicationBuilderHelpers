using Application.Server.Identity.Interfaces.Inbound;
using Asp.Versioning;
using Domain.Authorization.Constants;
using Domain.Authorization.Entities;
using Domain.Authorization.Models;
using Domain.Shared.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.WebApp.Server.Attributes;
using Presentation.WebApp.Server.Controllers.V1.Iam.PermissionsController.Requests;
using Presentation.WebApp.Server.Controllers.V1.Iam.PermissionsController.Responses;
using Presentation.WebApp.Server.Models.Shared;
using AuthorizationPermissions = Domain.Authorization.Constants.Permissions;

namespace Presentation.WebApp.Server.Controllers.V1.Iam.PermissionsController;

/// <summary>
/// IAM permission operations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{v:apiVersion}/iam")]
[Produces("application/json")]
[Authorize]
[Tags("IAM")]
public sealed class IamPermissionsController(
    IUserAuthorizationService userAuthorizationService) : ControllerBase
{
    /// <summary>
    /// Lists all available permissions in the system.
    /// </summary>
    /// <remarks>
    /// Returns a hierarchical tree of all permissions defined in the system.
    /// Each permission includes its path, identifier, description, and any child permissions.
    /// This endpoint is useful for building permission selection UIs or understanding the available authorization scopes.
    /// </remarks>
    /// <returns>The permission tree.</returns>
    /// <response code="200">Returns the hierarchical permission tree.</response>
    [HttpGet("permissions")]
    [ProducesResponseType<ListResponse<PermissionInfoResponse>>(StatusCodes.Status200OK)]
    public IActionResult ListPermissions()
    {
        var permissions = AuthorizationPermissions.PermissionTreeRoots
            .Select(MapToResponse)
            .ToList();

        return Ok(ListResponse<PermissionInfoResponse>.From(permissions));
    }

    /// <summary>
    /// Grants a direct permission to a user.
    /// </summary>
    /// <remarks>
    /// Grants a specific permission directly to a user, bypassing role-based assignment.
    /// Direct permissions take precedence over role-inherited permissions when evaluating access.
    /// The <c>isAllow</c> flag determines whether this is an allow or deny grant.
    /// Use deny grants to explicitly block access that would otherwise be allowed by roles.
    /// </remarks>
    /// <param name="request">The permission grant request containing user ID and permission identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content.</returns>
    /// <response code="204">Permission granted successfully.</response>
    /// <response code="400">Invalid permission identifier.</response>
    /// <response code="404">User not found.</response>
    [HttpPost("permissions/grant")]
    [RequiredPermission(PermissionIds.Api.Iam.Permissions.Grant.Identifier)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GrantPermission(
        [FromBody] PermissionGrantRequest request,
        CancellationToken cancellationToken)
    {
        var grantedBy = User.Identity?.Name;
        await userAuthorizationService.GrantPermissionAsync(
            request.UserId,
            request.PermissionIdentifier,
            request.IsAllow,
            request.Description,
            grantedBy,
            cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Revokes a direct permission from a user.
    /// </summary>
    /// <remarks>
    /// Removes a previously granted direct permission from a user.
    /// This only affects direct permission grants; role-inherited permissions remain unaffected.
    /// If the user has the permission through a role assignment, they will retain access after revocation.
    /// </remarks>
    /// <param name="request">The permission revocation request containing user ID and permission identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content.</returns>
    /// <response code="204">Permission revoked successfully.</response>
    /// <response code="400">Invalid permission identifier.</response>
    /// <response code="404">User or permission grant not found.</response>
    [HttpPost("permissions/revoke")]
    [RequiredPermission(PermissionIds.Api.Iam.Permissions.Revoke.Identifier)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RevokePermission(
        [FromBody] PermissionRevocationRequest request,
        CancellationToken cancellationToken)
    {
        var revoked = await userAuthorizationService.RevokePermissionAsync(
            request.UserId,
            request.PermissionIdentifier,
            cancellationToken);

        if (!revoked)
        {
            throw new EntityNotFoundException("Permission", request.PermissionIdentifier);
        }

        return NoContent();
    }

    private static PermissionInfoResponse MapToResponse(Permission permission)
    {
        return new PermissionInfoResponse
        {
            Path = permission.Path,
            Identifier = permission.Identifier,
            Description = permission.Description,
            Parameters = permission.Parameters,
            IsRead = permission.IsRead,
            IsWrite = permission.IsWrite,
            Children = permission.HasChildren
                ? permission.Permissions.Select(MapToResponse).ToList()
                : null
        };
    }
}

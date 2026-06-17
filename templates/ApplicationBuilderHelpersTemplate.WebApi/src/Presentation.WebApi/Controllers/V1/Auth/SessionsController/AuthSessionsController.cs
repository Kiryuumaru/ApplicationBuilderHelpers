using Application.Identity.Interfaces.Inbound;
using Asp.Versioning;
using Domain.Authorization.Constants;
using Domain.Shared.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Presentation.WebApi.Attributes;
using Presentation.WebApi.Controllers.V1.Auth.SessionsController.Responses;
using Presentation.WebApi.Extensions;
using Presentation.WebApi.Models.Shared;
using System.ComponentModel.DataAnnotations;

namespace Presentation.WebApi.Controllers.V1.Auth.SessionsController;

/// <summary>
/// Controller for user session management.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{v:apiVersion}/auth")]
[Produces("application/json")]
[Tags("Authentication")]
public sealed class AuthSessionsController(ISessionService sessionService) : ControllerBase
{
    /// <summary>
    /// Lists all active sessions for the user.
    /// </summary>
    /// <remarks>
    /// Returns all currently active sessions including device name, IP address, and last activity.
    /// The current session is marked with <c>isCurrent: true</c> for UI highlighting.
    /// Use this to review where the account is logged in before revoking sessions.
    /// </remarks>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of active sessions.</returns>
    /// <response code="200">Returns the list of sessions.</response>
    /// <response code="401">Not authenticated.</response>
    [HttpGet("users/{userId:guid}/sessions")]
    [RequiredPermission(PermissionIds.Api.Auth.Sessions.List.Identifier)]
    [ProducesResponseType<ListResponse<SessionInfoResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ListSessions(
        [FromRoute, Required, PermissionParameter(PermissionIds.Api.Auth.UserIdParameter)] Guid userId,
        CancellationToken cancellationToken)
    {
        var currentSessionId = User.GetSessionId();
        var sessions = await sessionService.GetActiveByUserIdAsync(userId, cancellationToken);

        var sessionInfos = sessions.Select(s => new SessionInfoResponse
        {
            Id = s.Id,
            DeviceName = s.DeviceName,
            UserAgent = s.UserAgent,
            IpAddress = s.IpAddress,
            CreatedAt = s.CreatedAt,
            LastUsedAt = s.LastUsedAt,
            IsCurrent = s.Id == currentSessionId
        }).ToList();

        return Ok(ListResponse<SessionInfoResponse>.From(sessionInfos));
    }

    /// <summary>
    /// Revokes a specific session.
    /// </summary>
    /// <remarks>
    /// Invalidates the specified session, logging out that device.
    /// The refresh token associated with the session will no longer work.
    /// You can revoke your own current session (equivalent to logout) or other sessions.
    /// </remarks>
    /// <param name="userId">The user ID.</param>
    /// <param name="id">The session ID to revoke.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success status.</returns>
    /// <response code="204">Session revoked successfully.</response>
    /// <response code="401">Not authenticated.</response>
    /// <response code="404">Session not found.</response>
    [HttpDelete("users/{userId:guid}/sessions/{id:guid}")]
    [RequiredPermission(PermissionIds.Api.Auth.Sessions.Revoke.Identifier)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeSession(
        [FromRoute, Required, PermissionParameter(PermissionIds.Api.Auth.UserIdParameter)] Guid userId,
        Guid id,
        CancellationToken cancellationToken)
    {
        var success = await sessionService.RevokeForUserAsync(userId, id, cancellationToken);
        if (!success)
        {
            throw new EntityNotFoundException("Session", id.ToString());
        }

        return NoContent();
    }

    /// <summary>
    /// Revokes all sessions for the user.
    /// </summary>
    /// <remarks>
    /// Logs out from all devices including the current session.
    /// Use this for security purposes when you suspect unauthorized access.
    /// Returns the count of sessions that were revoked.
    /// </remarks>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success status with count of revoked sessions.</returns>
    /// <response code="200">Sessions revoked successfully.</response>
    /// <response code="401">Not authenticated.</response>
    [HttpDelete("users/{userId:guid}/sessions")]
    [RequiredPermission(PermissionIds.Api.Auth.Sessions.RevokeAll.Identifier)]
    [ProducesResponseType<SessionRevokeAllResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RevokeAllSessions(
        [FromRoute, Required, PermissionParameter(PermissionIds.Api.Auth.UserIdParameter)] Guid userId,
        CancellationToken cancellationToken)
    {
        var revokedCount = await sessionService.RevokeAllForUserAsync(userId, cancellationToken);
        return Ok(new SessionRevokeAllResponse { RevokedCount = revokedCount });
    }
}

#if DEBUG
using Application.Server.Authorization.Interfaces.Inbound;
using Application.Server.Identity.Interfaces.Inbound;
using Application.Server.Identity.Models;
using Asp.Versioning;
using Domain.Authorization.Constants;
using Domain.Shared.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.WebApp.Server.Controllers.V1.DevTools.AdminController.Requests;
using Presentation.WebApp.Server.Controllers.V1.DevTools.AdminController.Responses;

namespace Presentation.WebApp.Server.Controllers.V1.DevTools.AdminController;

/// <summary>
/// DevTools-only controller for testing purposes.
/// This controller is only available in DEBUG builds.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{v:apiVersion}/devtools")]
[Produces("application/json")]
[Tags("DevTools")]
public sealed class DevToolsAdminController(
    IUserProfileService userProfileService,
    IUserAuthorizationService userAuthorizationService,
    IUserRegistrationService userRegistrationService) : ControllerBase
{
    /// <summary>
    /// Grants the ADMIN role to a user.
    /// </summary>
    /// <remarks>
    /// DEBUG builds only. Elevates an existing user to administrator status.
    /// This endpoint bypasses normal authorization checks for testing purposes.
    /// Requires the caller to be authenticated but does not require admin permissions.
    /// </remarks>
    /// <param name="userId">The user ID to grant admin access to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">Admin role granted successfully.</response>
    /// <response code="404">User not found.</response>
    [HttpPost("users/{userId:guid}/make-admin")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MakeAdmin(Guid userId, CancellationToken cancellationToken)
    {
        var user = await userProfileService.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            throw new EntityNotFoundException("User", userId.ToString());
        }

        await userAuthorizationService.AssignRoleAsync(
            userId,
            new RoleAssignmentRequest(Roles.Admin.Code, null),
            cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Creates an admin user in one step.
    /// </summary>
    /// <remarks>
    /// DEBUG builds only. Combines user registration and admin role assignment into a single operation.
    /// This endpoint is anonymous and does not require authentication, making it suitable for test bootstrapping.
    /// The created user will have full administrative privileges immediately.
    /// </remarks>
    /// <param name="request">The registration request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created user's ID.</returns>
    /// <response code="201">Admin user created successfully.</response>
    /// <response code="400">Invalid registration data.</response>
    /// <response code="409">Username or email already exists.</response>
    [HttpPost("create-admin")]
    [AllowAnonymous]
    [ProducesResponseType<CreateAdminResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateAdmin(
        [FromBody] CreateAdminRequest request,
        CancellationToken cancellationToken)
    {
        var user = await userRegistrationService.RegisterUserAsync(
            new UserRegistrationRequest(
                request.Username,
                request.Password,
                ConfirmPassword: null,
                Email: request.Email),
            cancellationToken);

        await userAuthorizationService.AssignRoleAsync(
            user.Id,
            new RoleAssignmentRequest(Roles.Admin.Code, null),
            cancellationToken);

        return Created(string.Empty, new CreateAdminResponse { UserId = user.Id });
    }
}
#endif

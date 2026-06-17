using Application.Identity.Interfaces.Inbound;
using Application.Identity.Models;
using Asp.Versioning;
using Domain.Authorization.Constants;
using Domain.Shared.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.WebApi.Attributes;
using Presentation.WebApi.Controllers.V1.Iam.UsersController.Requests;
using Presentation.WebApi.Controllers.V1.Iam.UsersController.Responses;
using Presentation.WebApi.Models.Shared;

namespace Presentation.WebApi.Controllers.V1.Iam.UsersController;

/// <summary>
/// IAM user operations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{v:apiVersion}/iam")]
[Produces("application/json")]
[Authorize]
[Tags("IAM")]
public sealed class IamUsersController(
    IUserProfileService userProfileService,
    IUserAuthorizationService userAuthorizationService,
    IUserRegistrationService userRegistrationService,
    IPasswordService passwordService) : ControllerBase
{
    /// <summary>
    /// Lists all users in the system.
    /// </summary>
    /// <remarks>
    /// Returns a paginated list of all registered users with their profile information.
    /// Includes user details such as email, phone, lockout status, and assigned role IDs.
    /// Requires administrative permission to access.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of all users.</returns>
    /// <response code="200">Returns the paginated list of users.</response>
    /// <response code="403">User lacks permission to list users.</response>
    [HttpGet("users")]
    [RequiredPermission(PermissionIds.Api.Iam.Users.List.Identifier)]
    [ProducesResponseType<PagedResponse<UserResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListUsers(CancellationToken cancellationToken)
    {
        var users = await userProfileService.ListAsync(cancellationToken);

        var items = users.Select(MapToResponse).ToList();

        return Ok(PagedResponse<UserResponse>.From(items, items.Count));
    }

    /// <summary>
    /// Gets a user by ID.
    /// </summary>
    /// <remarks>
    /// Retrieves detailed profile information for a specific user.
    /// Returns email, phone, two-factor status, lockout configuration, and assigned roles.
    /// Access may be restricted based on permission parameters (e.g., user can read their own profile).
    /// </remarks>
    /// <param name="id">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user details.</returns>
    /// <response code="200">Returns the user details.</response>
    /// <response code="404">User not found.</response>
    [HttpGet("users/{id:guid}")]
    [RequiredPermission(PermissionIds.Api.Iam.Users.ReadPermission.Identifier)]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUser(
        [PermissionParameter(PermissionIds.Api.Iam.Users.ReadPermission.UserIdParameter)] Guid id,
        CancellationToken cancellationToken)
    {
        var user = await userProfileService.GetByIdAsync(id, cancellationToken);

        if (user is null)
        {
            throw new EntityNotFoundException("User", id.ToString());
        }

        return Ok(MapToResponse(user));
    }

    /// <summary>
    /// Updates a user's profile.
    /// </summary>
    /// <remarks>
    /// Allows modification of user profile fields including email, phone number, and lockout settings.
    /// Username cannot be changed after registration.
    /// Only fields provided in the request body will be updated; omitted fields retain their current values.
    /// </remarks>
    /// <param name="id">The user ID.</param>
    /// <param name="request">The update request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated user.</returns>
    /// <response code="200">Returns the updated user.</response>
    /// <response code="403">User lacks permission to update this user.</response>
    /// <response code="404">User not found.</response>
    [HttpPut("users/{id:guid}")]
    [RequiredPermission(PermissionIds.Api.Iam.Users.Update.Identifier)]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateUser(
        [PermissionParameter(PermissionIds.Api.Iam.Users.Update.UserIdParameter)] Guid id,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        await userProfileService.UpdateUserAsync(id, new UserUpdateRequest
        {
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            LockoutEnabled = request.LockoutEnabled
        }, cancellationToken);

        var user = await userProfileService.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            throw new EntityNotFoundException("User", id.ToString());
        }

        return Ok(MapToResponse(user));
    }

    /// <summary>
    /// Deletes a user from the system.
    /// </summary>
    /// <remarks>
    /// Permanently removes a user account and all associated data.
    /// This action cannot be undone. All role assignments and direct permissions are also removed.
    /// Requires administrative permission to execute.
    /// </remarks>
    /// <param name="id">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content.</returns>
    /// <response code="204">User deleted successfully.</response>
    /// <response code="403">User lacks permission to delete users.</response>
    /// <response code="404">User not found.</response>
    [HttpDelete("users/{id:guid}")]
    [RequiredPermission(PermissionIds.Api.Iam.Users.Delete.Identifier)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteUser(
        Guid id,
        CancellationToken cancellationToken)
    {
        await userRegistrationService.DeleteUserAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Gets the effective permissions for a user.
    /// </summary>
    /// <remarks>
    /// Computes and returns the complete set of permissions the user has access to.
    /// This includes permissions inherited from assigned roles and any direct permission grants.
    /// Deny grants take precedence and will exclude permissions that would otherwise be allowed.
    /// </remarks>
    /// <param name="id">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The effective permissions.</returns>
    /// <response code="200">Returns the user's effective permissions.</response>
    /// <response code="404">User not found.</response>
    [HttpGet("users/{id:guid}/permissions")]
    [RequiredPermission(PermissionIds.Api.Iam.Users.Permissions.Identifier)]
    [ProducesResponseType<PermissionsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPermissions(
        [PermissionParameter(PermissionIds.Api.Iam.Users.Permissions.UserIdParameter)] Guid id,
        CancellationToken cancellationToken)
    {
        var permissions = await userAuthorizationService.GetEffectivePermissionsAsync(id, cancellationToken);

        return Ok(new PermissionsResponse
        {
            UserId = id,
            Permissions = permissions
        });
    }

    /// <summary>
    /// Resets a user's password.
    /// </summary>
    /// <remarks>
    /// Allows an administrator to set a new password for a user without requiring the old password.
    /// The new password must meet the configured password policy requirements.
    /// Use this for account recovery when users cannot reset their own passwords.
    /// </remarks>
    /// <param name="id">The user ID.</param>
    /// <param name="request">The password reset request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content.</returns>
    /// <response code="204">Password reset successfully.</response>
    /// <response code="400">New password doesn't meet requirements.</response>
    /// <response code="404">User not found.</response>
    [HttpPut("users/{id:guid}/password")]
    [RequiredPermission(PermissionIds.Api.Iam.Users.ResetPassword.Identifier)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword(
        Guid id,
        [FromBody] AdminResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        await passwordService.ResetPasswordAsync(id, request.NewPassword, cancellationToken);
        return NoContent();
    }

    private static UserResponse MapToResponse(UserDto user)
    {
        return new UserResponse
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            EmailConfirmed = user.EmailConfirmed,
            PhoneNumber = user.PhoneNumber,
            PhoneNumberConfirmed = user.PhoneNumberConfirmed,
            TwoFactorEnabled = user.TwoFactorEnabled,
            LockoutEnabled = user.LockoutEnabled,
            LockoutEnd = user.LockoutEnd,
            AccessFailedCount = user.AccessFailedCount,
            Created = user.Created,
            RoleIds = user.RoleIds.ToList()
        };
    }
}

using Application.Server.Authorization.Models;
using Application.Server.Authorization.Interfaces.Inbound;
using Application.Server.Identity.Interfaces.Inbound;
using Asp.Versioning;
using Domain.Authorization.Constants;
using Domain.Authorization.Entities;
using Domain.Authorization.Enums;
using Domain.Authorization.Models;
using Domain.Authorization.ValueObjects;
using Domain.Shared.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.WebApp.Server.Attributes;
using AppRoleAssignment = Application.Server.Identity.Models.RoleAssignmentRequest;
using Presentation.WebApp.Server.Controllers.V1.Iam.RolesController.Requests;
using Presentation.WebApp.Server.Controllers.V1.Iam.RolesController.Responses;
using Presentation.WebApp.Server.Models.Shared;

namespace Presentation.WebApp.Server.Controllers.V1.Iam.RolesController;

/// <summary>
/// IAM role operations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{v:apiVersion}/iam")]
[Produces("application/json")]
[Authorize]
[Tags("IAM")]
public sealed class IamRolesController(
    IUserAuthorizationService userAuthorizationService,
    IRoleService roleService) : ControllerBase
{
    /// <summary>
    /// Lists all available roles.
    /// </summary>
    /// <remarks>
    /// Returns both system-defined roles and custom roles created by administrators.
    /// Each role includes its code, name, description, and scope templates defining its permissions.
    /// System roles cannot be modified or deleted.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of available roles.</returns>
    /// <response code="200">Returns the list of all roles.</response>
    [HttpGet("roles")]
    [RequiredPermission(PermissionIds.Api.Iam.Roles.List.Identifier)]
    [ProducesResponseType<ListResponse<RoleInfoResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListRoles(CancellationToken cancellationToken)
    {
        var allRoles = await roleService.ListAsync(cancellationToken);

        var roles = allRoles.Select(MapToResponse).ToList();

        return Ok(ListResponse<RoleInfoResponse>.From(roles));
    }

    /// <summary>
    /// Gets a specific role by ID.
    /// </summary>
    /// <remarks>
    /// Retrieves complete role information including its scope templates.
    /// Scope templates define which permissions are granted (allow) or denied by the role.
    /// Parameter placeholders in scope templates are resolved when the role is assigned to a user.
    /// </remarks>
    /// <param name="roleId">The role ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The role details.</returns>
    /// <response code="200">Returns the role details.</response>
    /// <response code="404">Role not found.</response>
    [HttpGet("roles/{roleId:guid}")]
    [RequiredPermission(PermissionIds.Api.Iam.Roles.Read.Identifier)]
    [ProducesResponseType<RoleInfoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRole(Guid roleId, CancellationToken cancellationToken)
    {
        var role = await roleService.GetByIdAsync(roleId, cancellationToken);
        if (role is null)
        {
            throw new EntityNotFoundException("Role", roleId.ToString());
        }

        return Ok(MapToResponse(role));
    }

    /// <summary>
    /// Creates a new custom role.
    /// </summary>
    /// <remarks>
    /// Creates a custom role with specified scope templates defining its permission boundaries.
    /// The role code must be unique across all roles (system and custom).
    /// Scope templates can include parameter placeholders (e.g., <c>{userId}</c>) that are resolved during role assignment.
    /// </remarks>
    /// <param name="request">The role creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created role.</returns>
    /// <response code="201">Role created successfully.</response>
    /// <response code="400">Invalid role data.</response>
    /// <response code="409">Role with same code already exists.</response>
    [HttpPost("roles")]
    [RequiredPermission(PermissionIds.Api.Iam.Roles.Create.Identifier)]
    [ProducesResponseType<RoleInfoResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateRole(
        [FromBody] CreateRoleRequest request,
        CancellationToken cancellationToken)
    {
        var scopeTemplates = ParseScopeTemplates(request.ScopeTemplates);

        var descriptor = new RoleDescriptor(
            Code: request.Code,
            Name: request.Name,
            Description: request.Description,
            IsSystemRole: false,
            ScopeTemplates: scopeTemplates);

        var role = await roleService.CreateRoleAsync(descriptor, cancellationToken);

        return CreatedAtAction(
            nameof(GetRole),
            new { roleId = role.Id },
            MapToResponse(role));
    }

    /// <summary>
    /// Updates an existing custom role.
    /// </summary>
    /// <remarks>
    /// Allows modification of role metadata (name, description) and scope templates.
    /// System roles cannot be updated; only custom roles support modification.
    /// Changes to scope templates take effect immediately for all users assigned this role.
    /// </remarks>
    /// <param name="roleId">The role ID.</param>
    /// <param name="request">The role update request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated role.</returns>
    /// <response code="200">Returns the updated role.</response>
    /// <response code="400">Invalid role data or cannot modify system role.</response>
    /// <response code="404">Role not found.</response>
    [HttpPut("roles/{roleId:guid}")]
    [RequiredPermission(PermissionIds.Api.Iam.Roles.Update.Identifier)]
    [ProducesResponseType<RoleInfoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateRole(
        Guid roleId,
        [FromBody] UpdateRoleRequest request,
        CancellationToken cancellationToken)
    {
        var role = await roleService.UpdateMetadataAsync(roleId, request.Name, request.Description, cancellationToken);

        if (request.ScopeTemplates is not null)
        {
            var scopeTemplates = ParseScopeTemplates(request.ScopeTemplates);
            role = await roleService.ReplaceScopeTemplatesAsync(roleId, scopeTemplates, cancellationToken);
        }

        return Ok(MapToResponse(role));
    }

    /// <summary>
    /// Deletes a custom role.
    /// </summary>
    /// <remarks>
    /// Permanently removes a custom role from the system.
    /// System roles cannot be deleted. Users currently assigned this role will lose its permissions.
    /// This action cannot be undone.
    /// </remarks>
    /// <param name="roleId">The role ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content.</returns>
    /// <response code="204">Role deleted successfully.</response>
    /// <response code="400">Cannot delete system role.</response>
    /// <response code="404">Role not found.</response>
    [HttpDelete("roles/{roleId:guid}")]
    [RequiredPermission(PermissionIds.Api.Iam.Roles.Delete.Identifier)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteRole(Guid roleId, CancellationToken cancellationToken)
    {
        var deleted = await roleService.DeleteRoleAsync(roleId, cancellationToken);
        if (!deleted)
        {
            throw new EntityNotFoundException("Role", roleId.ToString());
        }

        return NoContent();
    }

    /// <summary>
    /// Assigns a role to a user.
    /// </summary>
    /// <remarks>
    /// Associates a role with a user, granting them the permissions defined by the role's scope templates.
    /// If the role has parameter placeholders, provide values in the <c>parameterValues</c> dictionary.
    /// A user can have multiple roles assigned; permissions are combined from all assigned roles.
    /// </remarks>
    /// <param name="request">The role assignment request containing user ID and role code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content.</returns>
    /// <response code="204">Role assigned successfully.</response>
    /// <response code="400">Invalid assignment data.</response>
    /// <response code="404">User or role not found.</response>
    [HttpPost("roles/assign")]
    [RequiredPermission(PermissionIds.Api.Iam.Roles.Assign.Identifier)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AssignRole(
        [FromBody] RoleAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        await userAuthorizationService.AssignRoleAsync(
            request.UserId,
            new AppRoleAssignment(request.RoleCode, request.ParameterValues),
            cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Removes a role from a user.
    /// </summary>
    /// <remarks>
    /// Revokes a role assignment from a user, removing the permissions granted by that role.
    /// Other role assignments and direct permissions remain unaffected.
    /// The role itself is not deleted; it can be reassigned to other users.
    /// </remarks>
    /// <param name="request">The role removal request containing user ID and role ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content.</returns>
    /// <response code="204">Role removed successfully.</response>
    /// <response code="400">Invalid removal data.</response>
    /// <response code="404">User or role assignment not found.</response>
    [HttpPost("roles/remove")]
    [RequiredPermission(PermissionIds.Api.Iam.Roles.Remove.Identifier)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RemoveRole(
        [FromBody] RoleRemovalRequest request,
        CancellationToken cancellationToken)
    {
        await userAuthorizationService.RemoveRoleAsync(request.UserId, request.RoleId, cancellationToken);
        return NoContent();
    }

    private static IReadOnlyCollection<ScopeTemplate> ParseScopeTemplates(IReadOnlyList<ScopeTemplateRequest>? templates)
    {
        if (templates is null || templates.Count == 0)
        {
            return [];
        }

        var scopeTemplates = new List<ScopeTemplate>();
        foreach (var template in templates)
        {
            var type = template.Type.ToLowerInvariant() switch
            {
                "allow" => ScopeDirectiveType.Allow,
                "deny" => ScopeDirectiveType.Deny,
                _ => throw new Domain.Shared.Exceptions.ValidationException($"Invalid scope template type: '{template.Type}'. Must be 'allow' or 'deny'.")
            };

            var parameters = template.Parameters?
                .Select(kvp => (kvp.Key, kvp.Value))
                .ToArray() ?? [];

            var scopeTemplate = type == ScopeDirectiveType.Allow
                ? ScopeTemplate.Allow(template.PermissionPath, parameters)
                : ScopeTemplate.Deny(template.PermissionPath, parameters);

            scopeTemplates.Add(scopeTemplate);
        }

        return scopeTemplates;
    }

    private static RoleInfoResponse MapToResponse(Role role)
    {
        var parameters = role.ScopeTemplates
            .SelectMany(st => st.RequiredParameters)
            .Distinct()
            .ToList();

        return new RoleInfoResponse
        {
            Id = role.Id,
            Code = role.Code,
            Name = role.Name,
            Description = role.Description,
            IsSystemRole = role.IsSystemRole,
            Parameters = parameters,
            ScopeTemplates = role.ScopeTemplates.Select(MapScopeTemplateToResponse).ToList()
        };
    }

    private static ScopeTemplateResponse MapScopeTemplateToResponse(ScopeTemplate template)
    {
        return new ScopeTemplateResponse
        {
            Type = template.Type.ToString().ToLowerInvariant(),
            PermissionPath = template.PermissionPath,
            Parameters = template.ParameterTemplates.Count > 0
                ? template.ParameterTemplates.ToDictionary(p => p.Key, p => p.ValueTemplate)
                : null
        };
    }
}

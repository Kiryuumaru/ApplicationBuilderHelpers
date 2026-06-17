using Application.Identity.Interfaces.Inbound;
using Asp.Versioning;
using Domain.Authorization.Constants;
using Domain.Shared.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Presentation.WebApi.Attributes;
using Presentation.WebApi.Controllers.V1.Auth.ApiKeysController.Requests;
using Presentation.WebApi.Controllers.V1.Auth.ApiKeysController.Responses;
using Presentation.WebApi.Models.Shared;
using System.ComponentModel.DataAnnotations;

namespace Presentation.WebApi.Controllers.V1.Auth.ApiKeysController;

/// <summary>
/// Controller for user API key management.
/// API keys allow programmatic access to the API without interactive login.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{v:apiVersion}/auth")]
[Produces("application/json")]
[Tags("Authentication")]
public sealed class AuthApiKeysController(IApiKeyService apiKeyService) : ControllerBase
{
    /// <summary>
    /// Lists all active API keys for the user.
    /// </summary>
    /// <remarks>
    /// Returns all non-revoked API keys including name, creation date, and last usage.
    /// Use this to review which API keys are active before revoking them.
    /// Note: The actual API key JWT cannot be retrieved after creation.
    /// </remarks>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of active API keys.</returns>
    /// <response code="200">Returns the list of API keys.</response>
    /// <response code="401">Not authenticated.</response>
    [HttpGet("users/{userId:guid}/api-keys")]
    [RequiredPermission(PermissionIds.Api.Auth.ApiKeys.List.Identifier)]
    [ProducesResponseType<ListResponse<ApiKeyInfoResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ListApiKeys(
        [FromRoute, Required, PermissionParameter(PermissionIds.Api.Auth.UserIdParameter)] Guid userId,
        CancellationToken cancellationToken)
    {
        var apiKeys = await apiKeyService.GetByUserIdAsync(userId, cancellationToken);

        var apiKeyInfos = apiKeys.Select(k => new ApiKeyInfoResponse
        {
            Id = k.Id,
            Name = k.Name,
            CreatedAt = k.CreatedAt,
            ExpiresAt = k.ExpiresAt,
            LastUsedAt = k.LastUsedAt
        }).ToList();

        return Ok(ListResponse<ApiKeyInfoResponse>.From(apiKeyInfos));
    }

    /// <summary>
    /// Creates a new API key.
    /// </summary>
    /// <remarks>
    /// Creates a new API key for programmatic access to the API.
    /// 
    /// **IMPORTANT:** The API key JWT is only returned once at creation time.
    /// Store it securely - it cannot be retrieved again.
    /// 
    /// API keys:
    /// - Have the same permissions as the user (via roles)
    /// - Cannot refresh tokens (cannot call /auth/refresh)
    /// - Cannot manage API keys (cannot call /auth/api-keys endpoints)
    /// - Can optionally expire at a specified date
    /// 
    /// Usage: Pass the key in the Authorization header as `Bearer {key}`
    /// </remarks>
    /// <param name="userId">The user ID.</param>
    /// <param name="request">The API key creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created API key with the secret JWT.</returns>
    /// <response code="201">API key created successfully. Contains the secret JWT (shown only once).</response>
    /// <response code="400">Invalid request (e.g., max keys limit reached, expiration in past).</response>
    /// <response code="401">Not authenticated.</response>
    [HttpPost("users/{userId:guid}/api-keys")]
    [RequiredPermission(PermissionIds.Api.Auth.ApiKeys.Create.Identifier)]
    [ProducesResponseType<CreateApiKeyResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateApiKey(
        [FromRoute, Required, PermissionParameter(PermissionIds.Api.Auth.UserIdParameter)] Guid userId,
        [FromBody] CreateApiKeyRequest request,
        CancellationToken cancellationToken)
    {
        // Validate expiration is in the future if provided
        if (request.ExpiresAt.HasValue && request.ExpiresAt.Value <= DateTimeOffset.UtcNow)
        {
            return Problem(
                title: "Invalid expiration date",
                detail: "Expiration date must be in the future.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        try
        {
            var (metadata, token) = await apiKeyService.CreateAsync(
                userId,
                request.Name,
                request.ExpiresAt,
                cancellationToken);

            var response = new CreateApiKeyResponse
            {
                Id = metadata.Id,
                Name = metadata.Name,
                Key = token,
                CreatedAt = metadata.CreatedAt,
                ExpiresAt = metadata.ExpiresAt
            };

            return CreatedAtAction(
                nameof(ListApiKeys),
                new { userId },
                response);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Maximum number"))
        {
            return Problem(
                title: "API key limit reached",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>
    /// Revokes an API key.
    /// </summary>
    /// <remarks>
    /// Revokes (soft-deletes) the specified API key.
    /// After revocation, any requests using the API key JWT will be rejected.
    /// This operation cannot be undone - a new API key must be created if needed.
    /// </remarks>
    /// <param name="userId">The user ID.</param>
    /// <param name="id">The API key ID to revoke.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success status.</returns>
    /// <response code="204">API key revoked successfully.</response>
    /// <response code="401">Not authenticated.</response>
    /// <response code="404">API key not found or already revoked.</response>
    [HttpDelete("users/{userId:guid}/api-keys/{id:guid}")]
    [RequiredPermission(PermissionIds.Api.Auth.ApiKeys.Revoke.Identifier)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeApiKey(
        [FromRoute, Required, PermissionParameter(PermissionIds.Api.Auth.UserIdParameter)] Guid userId,
        Guid id,
        CancellationToken cancellationToken)
    {
        var success = await apiKeyService.RevokeAsync(userId, id, cancellationToken);
        if (!success)
        {
            throw new EntityNotFoundException("ApiKey", id.ToString());
        }

        return NoContent();
    }
}

using Application.Identity.Interfaces.Inbound;
using Asp.Versioning;
using Domain.Identity.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.WebApi.Controllers.V1.Auth.TokenController.Requests;
using Presentation.WebApi.Controllers.V1.Auth.Shared;
using Presentation.WebApi.Controllers.V1.Auth.Shared.Responses;

namespace Presentation.WebApi.Controllers.V1.Auth.TokenController;

/// <summary>
/// Controller for token management endpoints.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{v:apiVersion}/auth")]
[Produces("application/json")]
[Tags("Authentication")]
public sealed class AuthTokenController(
    IUserTokenService userTokenService,
    AuthResponseFactory authResponseFactory) : ControllerBase
{
    /// <summary>
    /// Refreshes an expired access token.
    /// </summary>
    /// <remarks>
    /// Exchanges a valid refresh token for a new access/refresh token pair.
    /// The old refresh token is invalidated after use (rotation).
    /// If the refresh token is expired or revoked, the user must re-authenticate.
    /// </remarks>
    /// <param name="request">The refresh token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>New JWT access and refresh tokens.</returns>
    /// <response code="200">Returns new JWT tokens.</response>
    /// <response code="401">Invalid or expired refresh token.</response>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await userTokenService.RefreshTokensAsync(request.RefreshToken, cancellationToken);

        if (!result.Succeeded)
        {
            throw new RefreshTokenInvalidException(result.Error, result.ErrorDescription);
        }

        var refreshUserInfo = await authResponseFactory.CreateUserInfoAsync(result.UserId!.Value, cancellationToken);

        return Ok(new AuthResponse
        {
            AccessToken = result.Tokens!.AccessToken,
            RefreshToken = result.Tokens.RefreshToken,
            ExpiresIn = result.Tokens.ExpiresInSeconds,
            User = refreshUserInfo
        });
    }
}

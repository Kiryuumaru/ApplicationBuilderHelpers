using Application.Server.Authorization.Interfaces.Inbound;
using Application.Server.Authorization.Interfaces.Outbound;
using Application.Server.Identity.Interfaces.Inbound;
using Application.Server.Identity.Interfaces.Outbound;
using Application.Server.Identity.Models;
using Domain.Authorization.Constants;
using Domain.Authorization.Entities;
using Domain.Authorization.Models;
using Domain.Identity.Constants;
using Domain.Identity.Exceptions;
using Domain.Identity.Entities;
using Domain.Shared.Exceptions;
using System.Security.Claims;

namespace Application.Server.Identity.Services;

internal sealed class AuthenticationService(
    IUserRepository userRepository,
    IPasswordVerifier passwordVerifier,
    IUserRoleResolver userRoleResolver,
    ITwoFactorService twoFactorService,
    IUserTokenService userTokenService) : IAuthenticationService
{
    public async Task<CredentialValidationResult> ValidateCredentialsAsync(string usernameOrEmail, string password, CancellationToken cancellationToken)
    {
        // Try username first, then fall back to email lookup
        var user = await userRepository.FindByUsernameAsync(usernameOrEmail, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            user = await userRepository.FindByEmailAsync(usernameOrEmail, cancellationToken).ConfigureAwait(false);
        }

        if (user is null)
        {
            return CredentialValidationResult.Failed("Invalid username or password.");
        }

        if (string.IsNullOrEmpty(user.PasswordHash))
        {
            return CredentialValidationResult.Failed("Invalid username or password.");
        }

        if (!passwordVerifier.Verify(user.PasswordHash, password))
        {
            return CredentialValidationResult.Failed("Invalid username or password.");
        }

        // Check for 2FA requirement
        if (user.TwoFactorEnabled)
        {
            return CredentialValidationResult.TwoFactorRequired(user.Id);
        }

        var roleResolutions = await userRoleResolver.ResolveRolesAsync(user, cancellationToken).ConfigureAwait(false);
        var roleCodes = roleResolutions.Select(r => r.Code).ToArray();
        return CredentialValidationResult.Success(user.Id, user.UserName, user.Email, roleCodes, user.IsAnonymous);
    }

    public async Task<CredentialValidationResult> GetUserForSessionAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return CredentialValidationResult.Failed($"User with ID {userId} not found.");
        }

        var roleResolutions2 = await userRoleResolver.ResolveRolesAsync(user, cancellationToken).ConfigureAwait(false);
        var roleCodes2 = roleResolutions2.Select(r => r.Code).ToArray();
        return CredentialValidationResult.Success(user.Id, user.UserName, user.Email, roleCodes2, user.IsAnonymous);
    }

    public async Task<UserSessionDto> AuthenticateAsync(string username, string password, CancellationToken cancellationToken)
    {
        var result = await AuthenticateWithResultAsync(username, password, cancellationToken).ConfigureAwait(false);

        if (!result.Succeeded)
        {
            throw new AuthenticationException(result.ErrorMessage ?? "Authentication failed.");
        }

        if (result.RequiresTwoFactor)
        {
            // Safe: RequiresTwoFactor is only true when user exists and was found, so UserId is non-null
            throw new TwoFactorRequiredException(result.UserId!.Value);
        }

        return result.Session ?? throw new EntityNotFoundException("Session", "create");
    }

    public async Task<AuthenticationResultDto> AuthenticateWithResultAsync(string username, string password, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByUsernameAsync(username, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return AuthenticationResultDto.Failed("Invalid username or password.");
        }

        if (string.IsNullOrEmpty(user.PasswordHash))
        {
            return AuthenticationResultDto.Failed("Invalid username or password.");
        }

        if (!passwordVerifier.Verify(user.PasswordHash, password))
        {
            return AuthenticationResultDto.Failed("Invalid username or password.");
        }

        // Check for 2FA requirement
        if (user.TwoFactorEnabled)
        {
            return AuthenticationResultDto.TwoFactorRequired(user.Id);
        }

        var sessionDto = await CreateSessionForUserInternalAsync(user, cancellationToken).ConfigureAwait(false);
        return AuthenticationResultDto.Success(sessionDto);
    }

    public async Task<UserSessionDto> CreateSessionForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByIdAsync(userId, cancellationToken).ConfigureAwait(false)
            ?? throw new EntityNotFoundException("User", userId.ToString());

        return await CreateSessionForUserInternalAsync(user, cancellationToken).ConfigureAwait(false);
    }

    public async Task<UserSessionDto> Complete2faAuthenticationAsync(Guid userId, string code, CancellationToken cancellationToken)
    {
        // SECURITY: Single DB lookup - pass user to verification to avoid timing-based user enumeration
        // If we did two lookups (one here, one in Verify), existing users would take longer than non-existing
        var user = await userRepository.FindByIdAsync(userId, cancellationToken).ConfigureAwait(false);

        // Cast to access internal method - both services are in same assembly
        var twoFactorServiceImpl = (TwoFactorService)twoFactorService;
        var isValid = await twoFactorServiceImpl.VerifyCodeForUserAsync(user, code, cancellationToken).ConfigureAwait(false);

        // Check user existence AFTER verification to prevent timing attacks
        if (user is null)
        {
            throw new TwoFactorSessionInvalidException();
        }

        if (!isValid)
        {
            throw new InvalidTwoFactorCodeException();
        }

        return await CreateSessionForUserInternalAsync(user, cancellationToken).ConfigureAwait(false);
    }

    private async Task<UserSessionDto> CreateSessionForUserInternalAsync(User user, CancellationToken cancellationToken)
    {
        // Delegate all token generation to IUserTokenService
        var tokenResult = await userTokenService.CreateSessionWithTokensAsync(
            user.Id,
            deviceInfo: null, // Device info not available in this flow
            cancellationToken).ConfigureAwait(false);

        // Get formatted role claims for the session DTO
        var roleResolutions = await userRoleResolver.ResolveRolesAsync(user, cancellationToken).ConfigureAwait(false);
        var roleClaims = roleResolutions.Select(r => r.ToFormattedClaim()).OrderBy(r => r, StringComparer.Ordinal).ToArray();

        return new UserSessionDto
        {
            SessionId = tokenResult.SessionId,
            UserId = user.Id,
            Username = user.UserName,
            IsAnonymous = user.IsAnonymous,
            AccessToken = tokenResult.AccessToken,
            RefreshToken = tokenResult.RefreshToken,
            IssuedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(tokenResult.ExpiresInSeconds),
            Roles = roleClaims
        };
    }
}

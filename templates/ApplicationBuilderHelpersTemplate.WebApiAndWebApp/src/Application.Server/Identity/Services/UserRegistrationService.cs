using Application.Server.Authorization.Interfaces.Inbound;
using Application.Server.Authorization.Interfaces.Outbound;
using Application.Server.Authorization.Utilities;
using Application.Server.Identity.Interfaces.Inbound;
using Application.Server.Identity.Interfaces.Outbound;
using Application.Server.Identity.Models;
using Domain.Identity.Exceptions;
using Domain.Identity.Entities;
using Domain.Shared.Exceptions;

namespace Application.Server.Identity.Services;

internal sealed class UserRegistrationService(
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IUserRoleResolver userRoleResolver,
    IPasswordHashService passwordHashService,
    IPasswordStrengthValidator passwordStrengthValidator) : IUserRegistrationService
{
    public async Task<UserDto> RegisterUserAsync(UserRegistrationRequest? request, CancellationToken cancellationToken)
    {
        User user;

        if (request is null)
        {
            // Create anonymous user
            user = User.RegisterAnonymous();
        }
        else
        {
            // Validate password confirmation
            if (!request.PasswordsMatch())
            {
                throw new PasswordValidationException("Password and confirm password do not match.");
            }

            // Validate uniqueness
            var existingByUsername = await userRepository.FindByUsernameAsync(request.Username, cancellationToken).ConfigureAwait(false);
            if (existingByUsername is not null)
            {
                throw new DuplicateEntityException("Username", request.Username);
            }

            if (!string.IsNullOrEmpty(request.Email))
            {
                var existingByEmail = await userRepository.FindByEmailAsync(request.Email, cancellationToken).ConfigureAwait(false);
                if (existingByEmail is not null)
                {
                    throw new DuplicateEntityException("Email", request.Email);
                }
            }

            // Validate password strength
            user = User.Register(request.Username, request.Email);
            var passwordErrors = await passwordStrengthValidator.ValidateAsync(user, request.Password, cancellationToken).ConfigureAwait(false);
            if (passwordErrors.Count > 0)
            {
                throw new PasswordValidationException($"Invalid password: {string.Join("; ", passwordErrors)}");
            }

            user.SetPasswordHash(passwordHashService.Hash(user, request.Password));

            // Process additional role assignments from the request
            if (request.RoleAssignments is { Count: > 0 })
            {
                foreach (var roleAssignment in request.RoleAssignments)
                {
                    var role = await roleRepository.GetByCodeAsync(roleAssignment.RoleCode, cancellationToken).ConfigureAwait(false)
                        ?? throw new EntityNotFoundException("Role", roleAssignment.RoleCode);

                    // Validate required parameters
                    RoleValidationHelper.ValidateRoleParameters(role, roleAssignment.ParameterValues);

                    user.AssignRole(role.Id, roleAssignment.ParameterValues);
                }
            }

            // Process permission grants from the request
            if (request.PermissionIdentifiers is { Count: > 0 })
            {
                foreach (var permissionIdentifier in request.PermissionIdentifiers)
                {
                    var grant = Domain.Identity.ValueObjects.UserPermissionGrant.Allow(
                        permissionIdentifier,
                        description: "Granted during registration");
                    user.GrantPermission(grant);
                }
            }
        }

        // Assign default User role with userId parameter
        var userRoleId = Domain.Authorization.Constants.Roles.User.Id;
        user.AssignRole(userRoleId, new Dictionary<string, string?> { ["roleUserId"] = user.Id.ToString() });

        await userRepository.SaveAsync(user, cancellationToken).ConfigureAwait(false);

        var externalLogins = await userRepository.GetLoginsAsync(user.Id, cancellationToken).ConfigureAwait(false);
        var roleResolutions = await userRoleResolver.ResolveRolesAsync(user, cancellationToken).ConfigureAwait(false);
        var roleCodes = roleResolutions.Select(r => r.Code).ToArray();

        return user.ToDto(user.RoleIds, roleCodes, externalLogins);
    }

    public async Task<UserDto> RegisterExternalAsync(ExternalUserRegistrationRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Check if this external login already exists
        var existingUserId = await userRepository.FindUserByLoginAsync(request.Provider, request.ProviderSubject, cancellationToken).ConfigureAwait(false);
        if (existingUserId.HasValue)
        {
            throw new DuplicateEntityException("ExternalLogin", $"{request.Provider}:{request.ProviderSubject}");
        }

        var user = User.RegisterExternal(
            request.Username,
            request.Provider.ToString(),
            request.ProviderSubject,
            request.ProviderEmail,
            request.ProviderDisplayName,
            request.Email);

        // Assign default User role with userId parameter
        var userRoleId = Domain.Authorization.Constants.Roles.User.Id;
        user.AssignRole(userRoleId, new Dictionary<string, string?> { ["roleUserId"] = user.Id.ToString() });

        await userRepository.SaveAsync(user, cancellationToken).ConfigureAwait(false);

        // Add external login
        await userRepository.AddLoginAsync(
            user.Id,
            request.Provider,
            request.ProviderSubject,
            request.ProviderDisplayName,
            request.ProviderEmail,
            cancellationToken).ConfigureAwait(false);

        var externalLogins = await userRepository.GetLoginsAsync(user.Id, cancellationToken).ConfigureAwait(false);
        var roleResolutions = await userRoleResolver.ResolveRolesAsync(user, cancellationToken).ConfigureAwait(false);
        var roleCodes = roleResolutions.Select(r => r.Code).ToArray();

        return user.ToDto(user.RoleIds, roleCodes, externalLogins);
    }

    public async Task UpgradeAnonymousWithPasskeyAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByIdAsync(userId, cancellationToken).ConfigureAwait(false)
            ?? throw new EntityNotFoundException("User", userId.ToString());

        if (!user.IsAnonymous)
        {
            throw new ValidationException("userId", "User is not anonymous and cannot be upgraded.");
        }

        // For passkey upgrade, we don't need a username - just mark as non-anonymous
        user.UpgradeFromAnonymousWithPasskey();
        await userRepository.SaveAsync(user, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByIdAsync(userId, cancellationToken).ConfigureAwait(false)
            ?? throw new EntityNotFoundException("User", userId.ToString());

        await userRepository.DeleteAsync(userId, cancellationToken).ConfigureAwait(false);
    }
}

using Application.Authorization.Interfaces.Inbound;
using Application.Identity.Extensions;
using Application.Identity.Interfaces.Inbound;
using Application.Identity.Models;
using Domain.Identity.Interfaces;
using Domain.Identity.Models;
using Domain.Identity.Entities;
using Domain.Shared.Exceptions;

namespace Application.Identity.Services;

internal sealed class UserProfileService(
    IUserRepository userRepository,
    IIdentityUnitOfWork unitOfWork,
    IUserRoleResolver userRoleResolver) : IUserProfileService
{
    public async Task<UserDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return null;
        }

        return await MapToUserDtoAsync(user, cancellationToken).ConfigureAwait(false);
    }

    public async Task<UserDto?> GetByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByUsernameAsync(username, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return null;
        }

        return await MapToUserDtoAsync(user, cancellationToken).ConfigureAwait(false);
    }

    public async Task<UserDto?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByEmailAsync(email, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return null;
        }

        return await MapToUserDtoAsync(user, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyCollection<UserDto>> ListAsync(CancellationToken cancellationToken)
    {
        var users = await userRepository.ListAsync(cancellationToken).ConfigureAwait(false);
        var result = new List<UserDto>(users.Count);

        foreach (var user in users)
        {
            var dto = await MapToUserDtoAsync(user, cancellationToken).ConfigureAwait(false);
            result.Add(dto);
        }

        return result;
    }

    public async Task UpdateUserAsync(Guid userId, UserUpdateRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await userRepository.FindByIdAsync(userId, cancellationToken).ConfigureAwait(false)
            ?? throw new EntityNotFoundException("User", userId.ToString());

        // Apply updates
        if (request.Email is not null)
        {
            user.SetEmail(request.Email);
            user.SetNormalizedEmail(request.Email.ToUpperInvariant());
        }

        if (request.PhoneNumber is not null)
        {
            user.SetPhoneNumber(request.PhoneNumber);
        }

        if (request.LockoutEnabled.HasValue)
        {
            user.SetLockoutEnabled(request.LockoutEnabled.Value);
        }

        userRepository.Update(user);
        await unitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task ChangeUsernameAsync(Guid userId, string newUsername, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByIdAsync(userId, cancellationToken).ConfigureAwait(false)
            ?? throw new EntityNotFoundException("User", userId.ToString());

        // Check if username is already taken
        var existing = await userRepository.FindByUsernameAsync(newUsername, cancellationToken).ConfigureAwait(false);
        if (existing is not null && existing.Id != userId)
        {
            throw new DuplicateEntityException("Username", newUsername);
        }

        user.SetUserName(newUsername);
        user.SetNormalizedUserName(newUsername.ToUpperInvariant());
        userRepository.Update(user);
        await unitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task ChangeEmailAsync(Guid userId, string newEmail, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByIdAsync(userId, cancellationToken).ConfigureAwait(false)
            ?? throw new EntityNotFoundException("User", userId.ToString());

        // Check if email is already taken
        var existing = await userRepository.FindByEmailAsync(newEmail, cancellationToken).ConfigureAwait(false);
        if (existing is not null && existing.Id != userId)
        {
            throw new DuplicateEntityException("Email", newEmail);
        }

        user.SetEmail(newEmail);
        userRepository.Update(user);
        await unitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task LinkEmailAsync(Guid userId, string email, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByIdAsync(userId, cancellationToken).ConfigureAwait(false)
            ?? throw new EntityNotFoundException("User", userId.ToString());

        // Check if email is already taken
        var existing = await userRepository.FindByEmailAsync(email, cancellationToken).ConfigureAwait(false);
        if (existing is not null && existing.Id != userId)
        {
            throw new DuplicateEntityException("Email", email);
        }

        user.SetEmail(email);
        userRepository.Update(user);
        await unitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UnlinkEmailAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByIdAsync(userId, cancellationToken).ConfigureAwait(false)
            ?? throw new EntityNotFoundException("User", userId.ToString());

        user.SetEmail(null);
        userRepository.Update(user);
        await unitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UnlinkExternalLoginAsync(Guid userId, Domain.Identity.Enums.ExternalLoginProvider provider, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByIdAsync(userId, cancellationToken).ConfigureAwait(false)
            ?? throw new EntityNotFoundException("User", userId.ToString());

        userRepository.RemoveLogin(userId, provider);
        await unitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<UserDto> MapToUserDtoAsync(User user, CancellationToken cancellationToken)
    {
        var externalLogins = await userRepository.GetLoginsAsync(user.Id, cancellationToken).ConfigureAwait(false);
        var roleResolutions = await userRoleResolver.ResolveRolesAsync(user, cancellationToken).ConfigureAwait(false);
        var roleCodes = roleResolutions.Select(r => r.Code).ToArray();

        return user.ToDto(user.RoleIds, roleCodes, externalLogins);
    }
}

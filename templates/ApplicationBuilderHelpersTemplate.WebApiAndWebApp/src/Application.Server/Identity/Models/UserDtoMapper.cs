using Domain.Identity.Entities;
using Domain.Identity.Models;
using Domain.Identity.ValueObjects;

namespace Application.Server.Identity.Models;

/// <summary>
/// Extension methods for mapping domain entities to DTOs.
/// </summary>
public static class UserDtoMapper
{
    /// <summary>
    /// Maps a User domain entity to a UserDto.
    /// </summary>
    public static UserDto ToDto(
        this User user, 
        IReadOnlyCollection<Guid>? roleIds = null,
        IReadOnlyCollection<string>? roleCodes = null, 
        IReadOnlyCollection<ExternalLoginInfo>? externalLogins = null)
    {
        ArgumentNullException.ThrowIfNull(user);

        return new UserDto
        {
            Id = user.Id,
            Username = user.UserName,
            Email = user.Email,
            EmailConfirmed = user.EmailConfirmed,
            PhoneNumber = user.PhoneNumber,
            PhoneNumberConfirmed = user.PhoneNumberConfirmed,
            IsAnonymous = user.IsAnonymous,
            LinkedAt = user.LinkedAt,
            HasPassword = !string.IsNullOrEmpty(user.PasswordHash),
            TwoFactorEnabled = user.TwoFactorEnabled,
            LockoutEnabled = user.LockoutEnabled,
            LockoutEnd = user.LockoutEnd,
            AccessFailedCount = user.AccessFailedCount,
            Created = user.Created,
            RoleIds = roleIds ?? [],
            Roles = roleCodes ?? [],
            ExternalLogins = (externalLogins ?? []).Select(e => new ExternalLoginDto
            {
                Provider = e.Provider.ToString(),
                ProviderSubject = e.ProviderSubject,
                DisplayName = e.DisplayName,
                Email = e.Email,
                LinkedAt = e.LinkedAt
            }).ToArray()
        };
    }

    /// <summary>
    /// Maps a UserSession domain value object to a UserSessionDto.
    /// </summary>
    public static UserSessionDto ToDto(this UserSession session, Guid sessionId, string accessToken, string refreshToken)
    {
        ArgumentNullException.ThrowIfNull(session);

        return new UserSessionDto
        {
            SessionId = sessionId,
            UserId = session.UserId,
            Username = session.Username,
            IsAnonymous = session.IsAnonymous,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            IssuedAt = session.IssuedAt,
            ExpiresAt = session.ExpiresAt,
            Roles = session.RoleCodes.ToArray()
        };
    }
}

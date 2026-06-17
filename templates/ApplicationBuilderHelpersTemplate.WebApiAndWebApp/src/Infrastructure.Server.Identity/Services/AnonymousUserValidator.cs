using Domain.Identity.Entities;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Server.Identity.Services;

internal sealed class AnonymousUserValidator : IUserValidator<User>
{
    private readonly UserValidator<User> _defaultValidator = new();

    public Task<IdentityResult> ValidateAsync(UserManager<User> manager, User user)
    {
        if (user.IsAnonymous && string.IsNullOrEmpty(user.UserName))
        {
            return Task.FromResult(IdentityResult.Success);
        }

        return _defaultValidator.ValidateAsync(manager, user);
    }
}

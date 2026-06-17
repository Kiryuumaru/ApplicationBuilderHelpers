using Application.Server.Identity.Interfaces.Outbound;
using Domain.Identity.Entities;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Server.Identity.Services;

internal sealed class AspNetIdentityPasswordStrengthValidator(UserManager<User> userManager) : IPasswordStrengthValidator
{
    public async Task<IReadOnlyCollection<string>> ValidateAsync(User user, string password, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (string.IsNullOrWhiteSpace(password))
        {
            return ["Password cannot be empty."];
        }

        cancellationToken.ThrowIfCancellationRequested();

        var errors = new List<string>();
        foreach (var validator in userManager.PasswordValidators)
        {
            var result = await validator.ValidateAsync(userManager, user, password).ConfigureAwait(false);
            if (!result.Succeeded)
            {
                errors.AddRange(result.Errors.Select(static e => e.Description));
            }
        }

        return errors;
    }
}

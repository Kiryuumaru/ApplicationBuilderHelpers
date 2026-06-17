using Application.Server.Identity.Interfaces.Outbound;
using Domain.Identity.Entities;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Server.Identity.Services;

internal sealed class AspNetIdentityPasswordVerifier(IPasswordHasher<User> passwordHasher) : IPasswordVerifier
{
    private readonly IPasswordHasher<User> _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));

    public bool Verify(string passwordHash, string providedPassword)
    {
        if (string.IsNullOrWhiteSpace(passwordHash) || string.IsNullOrWhiteSpace(providedPassword))
        {
            return false;
        }

        var user = User.RegisterAnonymous();
        var result = _passwordHasher.VerifyHashedPassword(user, passwordHash, providedPassword);
        return result != PasswordVerificationResult.Failed;
    }
}

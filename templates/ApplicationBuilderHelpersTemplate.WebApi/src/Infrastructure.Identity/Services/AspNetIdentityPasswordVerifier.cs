using Domain.Identity.Interfaces;
using Domain.Identity.Models;
using Domain.Identity.Entities;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Identity.Services;

internal sealed class AspNetIdentityPasswordVerifier(IPasswordHasher<User> passwordHasher) : IPasswordVerifier
{
    public bool Verify(string passwordHash, string providedPassword)
    {
        if (string.IsNullOrWhiteSpace(passwordHash) || string.IsNullOrWhiteSpace(providedPassword))
        {
            return false;
        }

        var user = User.RegisterAnonymous();
        var result = passwordHasher.VerifyHashedPassword(user, passwordHash, providedPassword);
        return result != PasswordVerificationResult.Failed;
    }
}

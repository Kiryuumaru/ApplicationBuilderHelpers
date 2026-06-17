using Application.Server.Identity.Interfaces.Outbound;
using Domain.Identity.Entities;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Server.Identity.Services;

internal sealed class AspNetIdentityPasswordHashService(IPasswordHasher<User> passwordHasher) : IPasswordHashService
{
    public string Hash(User user, string password)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Password cannot be null or whitespace.", nameof(password));
        }

        return passwordHasher.HashPassword(user, password);
    }
}

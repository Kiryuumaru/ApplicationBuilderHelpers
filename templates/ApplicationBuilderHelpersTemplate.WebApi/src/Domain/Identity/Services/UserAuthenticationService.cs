using Domain.Identity.Exceptions;
using Domain.Identity.Interfaces;
using Domain.Identity.Models;
using Domain.Identity.Entities;

namespace Domain.Identity.Services;

/// <summary>
/// Domain service for authenticating users against stored credentials.
/// </summary>
public sealed class UserAuthenticationService
{
    private readonly IPasswordVerifier _passwordVerifier;

    public UserAuthenticationService(IPasswordVerifier passwordVerifier)
    {
        _passwordVerifier = passwordVerifier ?? throw new ArgumentNullException(nameof(passwordVerifier));
    }

    public void Authenticate(User user, string secret, DateTimeOffset? issuedAt = null)
    {
        ArgumentNullException.ThrowIfNull(user);

        var timestamp = issuedAt ?? DateTimeOffset.UtcNow;
        if (!user.CanAuthenticate(timestamp))
        {
            throw new AuthenticationException("User is not allowed to authenticate in the current state.");
        }

        if (user.PasswordHash is null)
        {
            throw new AuthenticationException("User does not have a password set.");
        }

        if (!_passwordVerifier.Verify(user.PasswordHash, secret))
        {
            user.RecordFailedLogin(timestamp);
            throw new AuthenticationException("Invalid credentials supplied.");
        }

        user.RecordSuccessfulLogin(timestamp);
    }
}

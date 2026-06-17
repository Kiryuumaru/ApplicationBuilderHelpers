using Application.Server.Identity.Interfaces.Outbound;
using Application.Server.Identity.Services;
using Domain.Identity.Exceptions;
using Domain.Identity.Entities;

namespace Application.UnitTests.Identity;

public class UserAuthenticationServiceTests
{
    [Fact]
    public void Authenticate_RecordsSuccessfulLogin()
    {
        var user = User.Register("trader", "trader@example.com");
        user.Activate();
        
        // Set password hash using reflection since setter is private
        var prop = typeof(User).GetProperty(nameof(User.PasswordHash));
        prop?.SetValue(user, "hashed_secret");

        var service = new UserAuthenticationService(new StubPasswordVerifier("secret"));
        service.Authenticate(user, "secret", DateTimeOffset.UtcNow);

        Assert.NotNull(user.LastLoginAt);
        Assert.Equal(0, user.AccessFailedCount);
    }

    [Fact]
    public void Authenticate_Throws_WhenSecretIsInvalid()
    {
        var user = User.Register("trader", "trader@example.com");
        user.Activate();
        
        // Set password hash
        var prop = typeof(User).GetProperty(nameof(User.PasswordHash));
        prop?.SetValue(user, "hashed_secret");

        var service = new UserAuthenticationService(new StubPasswordVerifier("secret"));

        Assert.Throws<AuthenticationException>(() => service.Authenticate(user, "invalid", DateTimeOffset.UtcNow));
        Assert.Equal(1, user.AccessFailedCount);
    }

    [Fact]
    public void Authenticate_Throws_WhenUserHasNoLocalCredential()
    {
        var user = User.Register("federated", "federated@example.com");
        user.Activate();

        var service = new UserAuthenticationService(new StubPasswordVerifier("secret"));

        Assert.Throws<AuthenticationException>(() => service.Authenticate(user, "secret", DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Authenticate_Throws_WhenLinkedUserHasNoPassword()
    {
        var user = User.Register("noPasswordUser", "np@example.com");
        user.LinkIdentity("github", "gh-42", "np@example.com", "NP");
        user.Activate();

        var service = new UserAuthenticationService(new StubPasswordVerifier("secret"));

        Assert.Throws<AuthenticationException>(() => service.Authenticate(user, "secret", DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Authenticate_AllowsLocalAuth_AfterPasswordSet()
    {
        var user = User.Register("maybeLocal", "local@example.com");
        user.LinkIdentity("google", "g-1", "local@example.com", "Local");
        user.Activate();

        // Set password hash using reflection since setter is private
        var prop = typeof(User).GetProperty(nameof(User.PasswordHash));
        prop?.SetValue(user, "hashed_secret");

        var service = new UserAuthenticationService(new StubPasswordVerifier("secret"));
        service.Authenticate(user, "secret", DateTimeOffset.UtcNow);

        Assert.NotNull(user.LastLoginAt);
    }

    private sealed class StubPasswordVerifier(string expected) : IPasswordVerifier
    {
        public bool Verify(string hashedPassword, string providedPassword)
        {
            return providedPassword == expected && hashedPassword == "hashed_secret";
        }
    }
}

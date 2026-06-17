using Domain.Identity.Interfaces;
using Domain.Identity.Models;
using Domain.Identity.Entities;
using Domain.Identity.Services;

namespace Domain.UnitTests.Identity;

public sealed class FederatedAuthTests
{
    [Fact]
    public void RegisterExternal_CreatesUserWithIdentityLink()
    {
        var user = User.RegisterExternal(
            "federatedUser",
            "google",
            "google-sub-1",
            providerEmail: "user@example.com",
            displayName: "User One",
            email: "user@example.com");

        Assert.Equal("federatedUser", user.UserName);
        Assert.Single(user.IdentityLinks);
        var link = user.IdentityLinks.First();
        Assert.Equal("google", link.Provider);
        Assert.Equal("google-sub-1", link.Subject);
        Assert.Equal("user@example.com", link.Email);
    }

    [Fact]
    public void RegisterExternal_AllowsNullEmail()
    {
        var user = User.RegisterExternal("anon", "github", "gh-anon");

        Assert.Null(user.Email);
        Assert.Single(user.IdentityLinks);
        Assert.Null(user.IdentityLinks.First().Email);
    }

    [Fact]
    public void LinkIdentity_Then_Authenticate_LocalFails_WhenNoPassword()
    {
        var user = User.Register("noPasswordUser", "np@example.com");
        user.LinkIdentity("github", "gh-42", "np@example.com", "NP");
        user.Activate();

        var service = new UserAuthenticationService(new StubPasswordVerifier("secret"));

        Assert.Throws<global::Domain.Identity.Exceptions.AuthenticationException>(() => service.Authenticate(user, "secret", DateTimeOffset.UtcNow));
    }

    [Fact]
    public void AddPasswordAfterLink_AllowsLocalAuth()
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

using Domain.Identity.Entities;
using Domain.Identity.ValueObjects;

namespace Domain.UnitTests.Identity;

public class FederatedAuthTests
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
}

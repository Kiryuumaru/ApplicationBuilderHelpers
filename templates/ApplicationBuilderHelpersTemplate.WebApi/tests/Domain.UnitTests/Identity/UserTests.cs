using Domain.Authorization.Entities;
using Domain.Authorization.ValueObjects;
using Domain.Identity.Exceptions;
using Domain.Identity.Interfaces;
using Domain.Identity.Models;
using Domain.Identity.Entities;
using Domain.Identity.Services;
using Domain.Identity.ValueObjects;
using Domain.Shared.Exceptions;

namespace Domain.UnitTests.Identity;

public sealed class UserTests
{
    [Fact]
    public void GrantPermission_NormalizesIdentifier()
    {
        var user = CreateUser();
        var grant = UserPermissionGrant.Allow(" api : iam : users : list ");

        user.GrantPermission(grant);

        var identifiers = user.GetPermissionIdentifiers();
        Assert.Single(identifiers);
        Assert.Equal("api:iam:users:list", identifiers.First());
    }

    [Fact]
    public void Authenticate_RecordsSuccessfulLogin()
    {
        var user = CreateUser();
        user.Activate();
        user.GrantPermission(UserPermissionGrant.Allow("api:iam:users:list"));
        
        // Set password hash
        var prop = typeof(User).GetProperty(nameof(User.PasswordHash));
        prop?.SetValue(user, "hashed_secret");

        var service = new UserAuthenticationService(new StubPasswordVerifier("secret"));
        service.Authenticate(user, "secret", DateTimeOffset.UtcNow);

        Assert.NotNull(user.LastLoginAt);
        Assert.Equal(0, user.AccessFailedCount);
    }

    [Fact]
    public void AuthenticationService_Throws_WhenSecretIsInvalid()
    {
        var user = CreateUser();
        user.Activate();
        
        // Set password hash
        var prop = typeof(User).GetProperty(nameof(User.PasswordHash));
        prop?.SetValue(user, "hashed_secret");

        var service = new UserAuthenticationService(new StubPasswordVerifier("secret"));

        Assert.Throws<AuthenticationException>(() => service.Authenticate(user, "invalid", DateTimeOffset.UtcNow));
        Assert.Equal(1, user.AccessFailedCount);
    }

    [Fact]
    public void AssignRole_AddsRoleIdOnce()
    {
        var user = CreateUser();
        var roleId = Guid.NewGuid();

        Assert.True(user.AssignRole(roleId));
        Assert.Contains(roleId, user.RoleIds);
        Assert.False(user.AssignRole(roleId));
    }

    [Fact]
    public void BuildEffectivePermissions_IncludesRoleGrants()
    {
        var user = CreateUser();
        user.GrantPermission(UserPermissionGrant.Allow("api:iam:users:list"));

        var role = Role.Create("admin", "Administrator");
        role.AddScopeTemplate(ScopeTemplate.Allow("api:iam:users:delete"));

        var identifiers = user.BuildEffectivePermissions([new UserRoleResolution(role)]);

        Assert.Contains("api:iam:users:list", identifiers);
        Assert.Contains("api:iam:users:delete", identifiers);
    }

    [Fact]
    public void RegisterExternal_AllowsMissingEmail_AndCanBeSetLater()
    {
        var user = User.RegisterExternal("ext-only", "github", "gh-1");

        Assert.Null(user.Email);
        user.SetEmail("final@example.com");

        Assert.Equal("final@example.com", user.Email);
    }

    [Fact]
    public void SetEmail_NormalizesAndOptionallyVerifies()
    {
        var user = User.Register("alpha");
        user.SetEmail("MixedCase@Example.com");

        Assert.Equal("mixedcase@example.com", user.Email);
        Assert.False(user.EmailConfirmed); // Was IsEmailVerified

        user.SetEmail("mixedcase@example.com", markVerified: true);

        Assert.True(user.EmailConfirmed);
    }

    [Fact]
    public void ClearEmail_RemovesVerification()
    {
        var user = User.Register("alpha", "user@example.com");
        user.MarkEmailVerified();

        user.ClearEmail();

        Assert.Null(user.Email);
        Assert.False(user.EmailConfirmed);
    }

    [Fact]
    public void MarkEmailVerified_ThrowsWhenEmailMissing()
    {
        var user = User.Register("alpha");

        Assert.Throws<DomainException>(() => user.MarkEmailVerified());
    }

    [Fact]
    public void AuthenticationService_Throws_WhenUserHasNoLocalCredential()
    {
        var user = User.Register("federated", "federated@example.com");
        user.Activate();

        var service = new UserAuthenticationService(new StubPasswordVerifier("secret"));

        Assert.Throws<AuthenticationException>(() => service.Authenticate(user, "secret", DateTimeOffset.UtcNow));
    }

    [Fact]
    public void LinkIdentity_AddsOrUpdatesLink()
    {
        var user = User.Register("social", "social@example.com");

        var firstLink = user.LinkIdentity("google", "123", "social@example.com", "Trader");
        Assert.Equal("google", firstLink.Provider);
        Assert.Single(user.IdentityLinks);

        var updatedLink = user.LinkIdentity("google", "123", "alt@example.com", "Trader Pro");
        Assert.Equal("alt@example.com", updatedLink.Email);
        Assert.Single(user.IdentityLinks);
        Assert.Equal("Trader Pro", user.IdentityLinks.First().DisplayName);
    }

    [Fact]
    public void UnlinkIdentity_RemovesLink()
    {
        var user = User.RegisterExternal("federated", "github", "abc123", email: "federated@example.com");
        Assert.Single(user.IdentityLinks);

        var removed = user.UnlinkIdentity("github", "abc123");

        Assert.True(removed);
        Assert.Empty(user.IdentityLinks);
    }

    private static User CreateUser()
    {
        return User.Register("trader", "trader@example.com");
    }

    private sealed class StubPasswordVerifier(string expected) : IPasswordVerifier
    {
        public bool Verify(string hashedPassword, string providedPassword)
        {
            return providedPassword == expected && hashedPassword == "hashed_secret";
        }
    }

}

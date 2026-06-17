using Application.Shared.Interfaces.Inbound;
using Application.Shared.Interfaces.Outbound;
using Application.Server.Authorization.Extensions;
using Application.Server.Authorization.Interfaces.Inbound;
using Application.Server.Authorization.Models;
using Application.Server.Identity.Extensions;
using Application.Server.Identity.Interfaces.Inbound;
using Application.Server.Identity.Models;
using Domain.Authorization.Constants;
using Domain.Authorization.Entities;
using Domain.Authorization.Models;
using Domain.Authorization.ValueObjects;
using Domain.Identity.Enums;
using Domain.Identity.Entities;
using Domain.Identity.Models;
using Domain.Identity.ValueObjects;
using Infrastructure.EFCore.Extensions;
using Infrastructure.EFCore.Server.Identity.Extensions;
using Infrastructure.EFCore.Sqlite.Extensions;
using Infrastructure.Server.Identity.Extensions;
using Infrastructure.Server.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using AuthenticationException = Domain.Identity.Exceptions.AuthenticationException;
using RolesConstants = Domain.Authorization.Constants.Roles;

namespace Application.IntegrationTests.Identity;

/// <summary>
/// Integration tests for Identity services (split from IIdentityService).
/// Uses real EF Core implementation via DI - persistence ignorant pattern.
/// </summary>
public class IdentityServiceTests
{
    [Fact]
    public async Task RegisterUserAsync_PersistsActivatedUserWithPermissions()
    {
        var fixture = CreateFixture();

        var request = new UserRegistrationRequest(
            Username: "alice",
            Password: "Password!23",
            Email: "alice@example.com",
            PermissionIdentifiers: [Permissions.RootReadIdentifier]);

        var user = await fixture.UserRegistrationService.RegisterUserAsync(request, CancellationToken.None);

        Assert.Equal("alice", user.Username);
        var stored = await fixture.UserProfileService.GetByUsernameAsync("alice", CancellationToken.None);
        Assert.NotNull(stored);
        
        // Check user has the User role assigned (roles now include inline parameters like "USER;roleUserId=...")
        Assert.Contains(stored!.Roles, r => r.StartsWith(RolesConstants.User.Code, StringComparison.OrdinalIgnoreCase));

        var session = await fixture.AuthenticationService.AuthenticateAsync("alice", "Password!23", CancellationToken.None);
        Assert.Contains(session.Roles, r => r.StartsWith(RolesConstants.User.Code, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task RegisterUserAsync_GrantsSelfScopedAccessByDefault()
    {
        var fixture = CreateFixture();

        var user = await fixture.UserRegistrationService.RegisterUserAsync(new UserRegistrationRequest("solo", "pa55word"), CancellationToken.None);

        var stored = await fixture.UserProfileService.GetByUsernameAsync("solo", CancellationToken.None);
        Assert.NotNull(stored);
        
        // User should have the User role assigned (roles now include inline parameters like "USER;roleUserId=...")
        Assert.Contains(stored!.Roles, r => r.StartsWith(RolesConstants.User.Code, StringComparison.OrdinalIgnoreCase));

        var session = await fixture.AuthenticationService.AuthenticateAsync("solo", "pa55word", CancellationToken.None);
        Assert.Contains(session.Roles, r => r.StartsWith(RolesConstants.User.Code, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task AuthenticateAsync_ReturnsSessionWithRolePermissions()
    {
        var fixture = CreateFixture();

        var request = new UserRegistrationRequest(
            Username: "admin-user",
            Password: "Secret!123",
            RoleAssignments: [new RoleAssignmentRequest(RolesConstants.Admin.Code)]);

        await fixture.UserRegistrationService.RegisterUserAsync(request, CancellationToken.None);

        var session = await fixture.AuthenticationService.AuthenticateAsync("admin-user", "Secret!123", CancellationToken.None);

        Assert.Contains(RolesConstants.Admin.Code, session.Roles);
    }

    [Fact]
    public async Task AuthenticateAsync_ThrowsForInvalidPassword()
    {
        var fixture = CreateFixture();

        await fixture.UserRegistrationService.RegisterUserAsync(new UserRegistrationRequest("bob", "correct-horse"), CancellationToken.None);

        await Assert.ThrowsAsync<AuthenticationException>(() => fixture.AuthenticationService.AuthenticateAsync("bob", "wrong", CancellationToken.None));
    }

    [Fact]
    public async Task AssignRoleAsync_AddsRoleToExistingUser()
    {
        var fixture = CreateFixture();

        var user = await fixture.UserRegistrationService.RegisterUserAsync(new UserRegistrationRequest("charlie", "pass"), CancellationToken.None);

        // Use Admin role which doesn't require parameters
        await fixture.UserAuthorizationService.AssignRoleAsync(
            user.Id,
            new RoleAssignmentRequest(RolesConstants.Admin.Code),
            CancellationToken.None);

        var session = await fixture.AuthenticationService.AuthenticateAsync("charlie", "pass", CancellationToken.None);

        Assert.Contains(RolesConstants.Admin.Code, session.Roles);
    }

    [Fact]
    public async Task AuthenticateAsync_UserReceivesRoleAndDirectPermissions()
    {
        var fixture = CreateFixture();

        const string additionalPermission = "api:favorites:read";
        var request = new UserRegistrationRequest(
            Username: "mixed-user",
            Password: "Mixed!123",
            PermissionIdentifiers: [additionalPermission],
            RoleAssignments: [new RoleAssignmentRequest(RolesConstants.Admin.Code)]);

        await fixture.UserRegistrationService.RegisterUserAsync(request, CancellationToken.None);

        var session = await fixture.AuthenticationService.AuthenticateAsync("mixed-user", "Mixed!123", CancellationToken.None);

        Assert.Contains(RolesConstants.Admin.Code, session.Roles);
        
        // Permissions are now fetched via userAuthorizationService, not on session
        // Permissions are returned as scope directives (e.g., "allow;_write")
        var permissions = await fixture.UserAuthorizationService.GetEffectivePermissionsAsync(session.UserId, CancellationToken.None);
        Assert.Contains($"allow;{Permissions.RootWriteIdentifier}", permissions); // from role grant
        Assert.Contains($"allow;{additionalPermission}", permissions); // direct user grant
    }

    [Fact]
    public async Task AssignRoleAsync_AddsCorrectRole()
    {
        var fixture = CreateFixture();

        var user = await fixture.UserRegistrationService.RegisterUserAsync(new UserRegistrationRequest("delta", "pass"), CancellationToken.None);

        // Use Admin role which doesn't require parameters
        await fixture.UserAuthorizationService.AssignRoleAsync(
            user.Id,
            new RoleAssignmentRequest(RolesConstants.Admin.Code),
            CancellationToken.None);

        var stored = await fixture.UserProfileService.GetByUsernameAsync("delta", CancellationToken.None);
        Assert.NotNull(stored);
        Assert.Contains(RolesConstants.Admin.Code, stored!.Roles);
    }

    [Fact]
    public async Task AuthenticateAsync_ExpandsParameterizedRoleAssignments()
    {
        var fixture = CreateFixture();

        var descriptor = new RoleDescriptor(
            Code: "portfolio_reader",
            Name: "Portfolio Reader",
            Description: "Reads a single portfolio",
            IsSystemRole: false,
            ScopeTemplates:
            [
                ScopeTemplate.Allow(
                    "api:trading:orders:read",
                    ("userId", "{userId}"))
            ]);

        await fixture.RoleService.CreateRoleAsync(descriptor, CancellationToken.None);

        var request = new UserRegistrationRequest(
            Username: "scoped-user",
            Password: "Scoped!123",
            RoleAssignments:
            [
                new RoleAssignmentRequest(
                    "portfolio_reader",
                    new Dictionary<string, string?> { ["userId"] = "user-123" })
            ]);

        await fixture.UserRegistrationService.RegisterUserAsync(request, CancellationToken.None);

        var session = await fixture.AuthenticationService.AuthenticateAsync("scoped-user", "Scoped!123", CancellationToken.None);
        
        // Role codes are normalized to uppercase and include inline parameters like "PORTFOLIO_READER;userId=user-123"
        Assert.Contains(session.Roles, r => r.StartsWith("PORTFOLIO_READER", StringComparison.OrdinalIgnoreCase));

        var stored = await fixture.UserProfileService.GetByUsernameAsync("scoped-user", CancellationToken.None);
        Assert.NotNull(stored);
        Assert.Contains(stored!.Roles, r => r.StartsWith("PORTFOLIO_READER", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(stored.Roles, r => r.StartsWith(RolesConstants.User.Code, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task AssignRoleAsync_ThrowsWhenRequiredParametersMissing()
    {
        var fixture = CreateFixture();

        // Create a user who doesn't yet have the User role assigned manually
        var user = await fixture.UserRegistrationService.RegisterUserAsync(new UserRegistrationRequest("epsilon", "pass"), CancellationToken.None);

        // Try to assign User role without providing the required roleUserId parameter
        // (Note: registration already assigns User role with correct params, 
        // but trying to assign it again without params should fail)
        await Assert.ThrowsAsync<Domain.Shared.Exceptions.ValidationException>(() => fixture.UserAuthorizationService.AssignRoleAsync(
            user.Id,
            new RoleAssignmentRequest(RolesConstants.User.Code), // Missing roleUserId parameter
            CancellationToken.None));
    }

    [Fact]
    public async Task RegisterExternalAsync_PersistsExternalLoginAndDefaultRole()
    {
        var fixture = CreateFixture();

        var request = new ExternalUserRegistrationRequest(
            Username: "oauth-alice",
            Provider: ExternalLoginProvider.GitHub,
            ProviderSubject: "gh-123",
            ProviderEmail: "alice@github.com",
            ProviderDisplayName: "Alice GH",
            Email: "alice@sample.com",
            PermissionIdentifiers: [Permissions.RootReadIdentifier]);

        var user = await fixture.UserRegistrationService.RegisterExternalAsync(request, CancellationToken.None);

        var stored = await fixture.UserProfileService.GetByUsernameAsync("oauth-alice", CancellationToken.None);
        Assert.NotNull(stored);
        Assert.Single(stored!.ExternalLogins);
        Assert.Equal("GitHub", stored.ExternalLogins.Single().Provider);
        Assert.Equal("gh-123", stored.ExternalLogins.Single().ProviderSubject);

        // User should have the User role assigned
        Assert.Contains(RolesConstants.User.Code, stored.Roles);
    }

    [Fact]
    public async Task RegisterExternalAsync_PreventsDuplicateProviderSubject()
    {
        var fixture = CreateFixture();

        var request = new ExternalUserRegistrationRequest(
            Username: "oauth-bob",
            Provider: ExternalLoginProvider.Google,
            ProviderSubject: "google-42");

        await fixture.UserRegistrationService.RegisterExternalAsync(request, CancellationToken.None);

        await Assert.ThrowsAsync<Domain.Shared.Exceptions.DuplicateEntityException>(() => fixture.UserRegistrationService.RegisterExternalAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task RegisterExternalAsync_ThrowsWhenProviderSubjectMissing()
    {
        var fixture = CreateFixture();

        await Assert.ThrowsAsync<Domain.Shared.Exceptions.DomainException>(() => fixture.UserRegistrationService.RegisterExternalAsync(
            new ExternalUserRegistrationRequest("name", ExternalLoginProvider.Google, ""),
            CancellationToken.None));
    }

    private static IdentityFixture CreateFixture()
    {
        // Use shared memory database with EF Core
        var dbName = Guid.NewGuid().ToString();
        var connectionString = $"Data Source={dbName};Mode=Memory;Cache=Shared";

        // Build configuration with connection string
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SQLITE_CONNECTION_STRING"] = connectionString
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        
        // Add authentication services required by SignInManager
        services.AddAuthentication();
        
        // Register stub IApplicationConstants for tests
        services.AddSingleton<IApplicationConstants>(new TestApplicationConstants());
        
        // Add JWT token services with a test configuration
        services.AddJwtTokenServices((sp, ct) =>
        {
            return Task.FromResult(new JwtConfiguration
            {
                Issuer = "TestIssuer",
                Audience = "TestAudience",
                Secret = "ThisIsATestSecretKeyThatIsLongEnoughForHS256Algorithm",
                DefaultExpiration = TimeSpan.FromHours(1),
                ClockSkew = TimeSpan.FromMinutes(5)
            });
        });
        
        services.AddAuthorizationServices();
        services.AddIdentityServices();

        services.AddIdentityCoreServices();

        // Register EF Core infrastructure (includes EFCoreDatabaseInitializationState)
        services.AddEFCoreInfrastructure();
        services.AddEFCoreSqlite();
        services.AddEFCoreIdentity();
        
        // Relax password requirements for tests
        services.Configure<IdentityOptions>(options =>
        {
            options.Password.RequireDigit = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequiredLength = 1;
            options.Password.RequiredUniqueChars = 0;
        });
        
        var provider = services.BuildServiceProvider();

        // Ensure the keep-alive connection is open before any database operations
        var connectionHolder = provider.GetRequiredService<Infrastructure.EFCore.Sqlite.Services.SqliteConnectionHolder>();
        connectionHolder.EnsureOpen();

        // Keep a connection open to preserve the in-memory database and run migrations
        var dbContextFactory = provider.GetRequiredService<Microsoft.EntityFrameworkCore.IDbContextFactory<Infrastructure.EFCore.EFCoreDbContext>>();
        var dbContext = dbContextFactory.CreateDbContext();
        dbContext.Database.EnsureCreated();
        
        // Note: Built-in roles (Admin, User) are served from static constants in Domain.Authorization.Constants.Roles
        // and do not need to be seeded in the database. The EFCoreRoleRepository checks static roles first.
        
        // Signal that database is initialized (uses internal method via InternalsVisibleTo)
        var initState = provider.GetRequiredService<Infrastructure.EFCore.Services.EFCoreDatabaseInitializationState>();
        initState.MarkInitialized();
        
        var userRegistrationService = provider.GetRequiredService<IUserRegistrationService>();
        var authenticationService = provider.GetRequiredService<IAuthenticationService>();
        var userProfileService = provider.GetRequiredService<IUserProfileService>();
        var userAuthorizationService = provider.GetRequiredService<IUserAuthorizationService>();
        var roleService = provider.GetRequiredService<IRoleService>();
        
        return new IdentityFixture(userRegistrationService, authenticationService, userProfileService, userAuthorizationService, roleService, dbContext);
    }

    private sealed record IdentityFixture(
        IUserRegistrationService UserRegistrationService,
        IAuthenticationService AuthenticationService,
        IUserProfileService UserProfileService,
        IUserAuthorizationService UserAuthorizationService,
        IRoleService RoleService,
        IDisposable Connection) : IDisposable
    {
        public void Dispose()
        {
            Connection.Dispose();
        }
    }

    private sealed class TestApplicationConstants : IApplicationConstants
    {
        public string AppName => "TestApp";
        public string AppTitle => "Test Application";
        public string AppDescription => "Test application for integration tests";
        public string Version => "1.0.0";
        public string AppTag => "test";
        public string BuildPayload => string.Empty;
    }
}

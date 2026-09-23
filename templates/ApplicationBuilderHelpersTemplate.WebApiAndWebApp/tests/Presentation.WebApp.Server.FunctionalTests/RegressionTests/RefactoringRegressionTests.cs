using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Presentation.WebApp.Server.FunctionalTests.RegressionTests;

/// <summary>
/// Auth regression guard: password linking, permissions in auth responses,
/// refresh-token theft detection, session counts, and role assignments.
/// </summary>
public class RefactoringRegressionTests : WebAppTestBase
{
    public RefactoringRegressionTests(ITestOutputHelper output) : base(output)
    {
    }

    #region Password linking

    /// <summary>
    /// The LinkPassword endpoint is for converting ANONYMOUS users to full accounts.
    /// Users who registered with a password should NOT be able to use LinkPassword.
    /// </summary>
    [TimedFact]
    public async Task LinkPassword_WhenUserAlreadyHasPassword_ReturnsBadRequest()
    {
        // Arrange
        var username = $"issue2_haspass_{Guid.NewGuid():N}";
        var email = $"{username}@example.com";

        var registerResponse = await HttpClient.PostAsJsonAsync("/api/v1/auth/register", new
        {
            Username = username,
            Email = email,
            Password = TestPassword,
            ConfirmPassword = TestPassword
        });
        
        var regContent = await registerResponse.Content.ReadAsStringAsync();
        Output.WriteLine($"Register response: {regContent}");
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        var registerResult = JsonSerializer.Deserialize<AuthResponse>(regContent, JsonOptions);
        var userId = registerResult!.User!.Id;
        var accessToken = registerResult.AccessToken;

        // Act
        HttpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        var linkResponse = await HttpClient.PostAsJsonAsync($"/api/v1/auth/users/{userId}/identity/password", new
        {
            Username = username,
            Password = "Another@Pass456!",
            ConfirmPassword = "Another@Pass456!"
        });
        HttpClient.DefaultRequestHeaders.Authorization = null;

        var linkContent = await linkResponse.Content.ReadAsStringAsync();
        Output.WriteLine($"LinkPassword response (status={linkResponse.StatusCode}): {linkContent}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, linkResponse.StatusCode);
    }

    #endregion

    #region Auth permissions

    /// <summary>
    /// Auth responses should return actual permissions, not empty array.
    /// After login, the User.Permissions array should contain the user's effective permissions.
    /// </summary>
    [TimedFact]
    public async Task Login_ReturnsNonEmptyPermissionsArray()
    {
        // Arrange
        var username = $"issue3_perms_{Guid.NewGuid():N}";
        var email = $"{username}@example.com";

        await HttpClient.PostAsJsonAsync("/api/v1/auth/register", new
        {
            Username = username,
            Email = email,
            Password = TestPassword,
            ConfirmPassword = TestPassword
        });

        // Act
        var loginResponse = await HttpClient.PostAsJsonAsync("/api/v1/auth/login", new
        {
            Username = username,
            Password = TestPassword
        });

        var content = await loginResponse.Content.ReadAsStringAsync();
        Output.WriteLine($"Login response: {content}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var loginResult = JsonSerializer.Deserialize<AuthResponse>(content, JsonOptions);
        Assert.NotNull(loginResult);
        Assert.NotNull(loginResult!.User);
        Assert.NotNull(loginResult.User!.Permissions);
        
        Output.WriteLine($"Permissions count: {loginResult.User.Permissions.Length}");
        foreach (var perm in loginResult.User.Permissions)
        {
            Output.WriteLine($"  Permission: {perm}");
        }

        Assert.True(loginResult.User.Permissions.Length > 0, 
            "Permissions array should NOT be empty - user should have at least USER role permissions");
    }

    /// <summary>
    /// Register response should also return permissions in user object.
    /// </summary>
    [TimedFact]
    public async Task Register_ReturnsNonEmptyPermissionsArray()
    {
        // Arrange
        var username = $"issue3_reg_{Guid.NewGuid():N}";
        var email = $"{username}@example.com";

        // Act
        var registerResponse = await HttpClient.PostAsJsonAsync("/api/v1/auth/register", new
        {
            Username = username,
            Email = email,
            Password = TestPassword,
            ConfirmPassword = TestPassword
        });

        var content = await registerResponse.Content.ReadAsStringAsync();
        Output.WriteLine($"Register response: {content}");

        // Assert
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        var result = JsonSerializer.Deserialize<AuthResponse>(content, JsonOptions);
        Assert.NotNull(result);
        Assert.NotNull(result!.User);
        Assert.NotNull(result.User!.Permissions);
        
        Output.WriteLine($"Permissions count: {result.User.Permissions.Length}");

        Assert.True(result.User.Permissions.Length > 0,
            "Permissions array should NOT be empty after registration");
    }

    /// <summary>
    /// Refresh token response should return permissions in user object.
    /// </summary>
    [TimedFact]
    public async Task Refresh_ReturnsNonEmptyPermissionsArray()
    {
        // Arrange
        var username = $"issue3_refresh_{Guid.NewGuid():N}";
        var email = $"{username}@example.com";

        var registerResponse = await HttpClient.PostAsJsonAsync("/api/v1/auth/register", new
        {
            Username = username,
            Email = email,
            Password = TestPassword,
            ConfirmPassword = TestPassword
        });
        var regContent = await registerResponse.Content.ReadAsStringAsync();
        var registerResult = JsonSerializer.Deserialize<AuthResponse>(regContent, JsonOptions);
        var refreshToken = registerResult!.RefreshToken;

        // Act
        var refreshResponse = await HttpClient.PostAsJsonAsync("/api/v1/auth/refresh", new
        {
            RefreshToken = refreshToken
        });

        var content = await refreshResponse.Content.ReadAsStringAsync();
        Output.WriteLine($"Refresh response: {content}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);

        var result = JsonSerializer.Deserialize<AuthResponse>(content, JsonOptions);
        Assert.NotNull(result);
        Assert.NotNull(result!.User);
        Assert.NotNull(result.User!.Permissions);
        Assert.True(result.User.Permissions.Length > 0,
            "Permissions array should NOT be empty after refresh");
    }

    #endregion

    #region Token theft detection

    /// <summary>
    /// Using a rotated refresh token again should be rejected.
    /// </summary>
    [TimedFact]
    public async Task TokenTheftDetection_OldRefreshToken_IsRejected()
    {
        // Arrange
        var username = $"issue5_theft_{Guid.NewGuid():N}";
        var email = $"{username}@example.com";

        var registerResponse = await HttpClient.PostAsJsonAsync("/api/v1/auth/register", new
        {
            Username = username,
            Email = email,
            Password = TestPassword,
            ConfirmPassword = TestPassword
        });
        var regContent = await registerResponse.Content.ReadAsStringAsync();
        var registerResult = JsonSerializer.Deserialize<AuthResponse>(regContent, JsonOptions);
        var originalRefreshToken = registerResult!.RefreshToken;

        // First refresh - get new tokens
        var firstRefreshResponse = await HttpClient.PostAsJsonAsync("/api/v1/auth/refresh", new
        {
            RefreshToken = originalRefreshToken
        });
        Assert.Equal(HttpStatusCode.OK, firstRefreshResponse.StatusCode);

        // Act
        var theftAttemptResponse = await HttpClient.PostAsJsonAsync("/api/v1/auth/refresh", new
        {
            RefreshToken = originalRefreshToken
        });

        // Assert
        var errorContent = await theftAttemptResponse.Content.ReadAsStringAsync();
        Output.WriteLine($"Theft attempt status: {theftAttemptResponse.StatusCode}");
        Output.WriteLine($"Theft attempt response: {errorContent}");

        Assert.Equal(HttpStatusCode.Unauthorized, theftAttemptResponse.StatusCode);
    }

    /// <summary>
    /// After theft detection, the rotated refresh token should be invalid
    /// because the entire session is revoked.
    /// </summary>
    [TimedFact]
    public async Task TokenTheftDetection_RevokesEntireSession()
    {
        // Arrange
        var username = $"issue5_revoke_{Guid.NewGuid():N}";
        var email = $"{username}@example.com";

        var registerResponse = await HttpClient.PostAsJsonAsync("/api/v1/auth/register", new
        {
            Username = username,
            Email = email,
            Password = TestPassword,
            ConfirmPassword = TestPassword
        });
        var regContent = await registerResponse.Content.ReadAsStringAsync();
        var registerResult = JsonSerializer.Deserialize<AuthResponse>(regContent, JsonOptions);
        var originalRefreshToken = registerResult!.RefreshToken;

        // Legitimate user refreshes first
        var firstRefreshResponse = await HttpClient.PostAsJsonAsync("/api/v1/auth/refresh", new
        {
            RefreshToken = originalRefreshToken
        });
        Assert.Equal(HttpStatusCode.OK, firstRefreshResponse.StatusCode);
        var firstRefreshContent = await firstRefreshResponse.Content.ReadAsStringAsync();
        var firstRefreshResult = JsonSerializer.Deserialize<AuthResponse>(firstRefreshContent, JsonOptions);
        var newRefreshToken = firstRefreshResult!.RefreshToken;

        // Reuse the rotated token
        var theftAttemptResponse = await HttpClient.PostAsJsonAsync("/api/v1/auth/refresh", new
        {
            RefreshToken = originalRefreshToken
        });
        Assert.Equal(HttpStatusCode.Unauthorized, theftAttemptResponse.StatusCode);

        // Act
        var newTokenAttemptResponse = await HttpClient.PostAsJsonAsync("/api/v1/auth/refresh", new
        {
            RefreshToken = newRefreshToken
        });

        // Assert
        Output.WriteLine($"New token attempt status: {newTokenAttemptResponse.StatusCode}");
        Assert.Equal(HttpStatusCode.Unauthorized, newTokenAttemptResponse.StatusCode);
    }

    #endregion

    #region Sessions

    /// <summary>
    /// Each registration should create exactly ONE session.
    /// </summary>
    [TimedFact]
    public async Task Register_CreatesExactlyOneSession()
    {
        // Arrange
        var username = $"issue6_single_{Guid.NewGuid():N}";
        var email = $"{username}@example.com";

        // Act
        var registerResponse = await HttpClient.PostAsJsonAsync("/api/v1/auth/register", new
        {
            Username = username,
            Email = email,
            Password = TestPassword,
            ConfirmPassword = TestPassword
        });
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        var regContent = await registerResponse.Content.ReadAsStringAsync();
        var registerResult = JsonSerializer.Deserialize<AuthResponse>(regContent, JsonOptions);
        var userId = registerResult!.User!.Id;
        var accessToken = registerResult.AccessToken;

        // Get sessions count
        HttpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        var sessionsResponse = await HttpClient.GetAsync($"/api/v1/auth/users/{userId}/sessions");
        HttpClient.DefaultRequestHeaders.Authorization = null;

        var sessionsContent = await sessionsResponse.Content.ReadAsStringAsync();
        Output.WriteLine($"Sessions response: {sessionsContent}");
        Assert.Equal(HttpStatusCode.OK, sessionsResponse.StatusCode);

        var sessionsResult = JsonSerializer.Deserialize<SessionsResponse>(sessionsContent, JsonOptions);
        
        Output.WriteLine($"Sessions count after register: {sessionsResult?.Items?.Length ?? 0}");

        // Assert
        Assert.Single(sessionsResult!.Items!);
    }

    /// <summary>
    /// Multiple logins create one session per login.
    /// </summary>
    [TimedFact]
    public async Task MultipleLogins_CreatesCorrectNumberOfSessions()
    {
        // Arrange
        var username = $"issue6_multi_{Guid.NewGuid():N}";
        var email = $"{username}@example.com";

        var registerResponse = await HttpClient.PostAsJsonAsync("/api/v1/auth/register", new
        {
            Username = username,
            Email = email,
            Password = TestPassword,
            ConfirmPassword = TestPassword
        });
        var regContent = await registerResponse.Content.ReadAsStringAsync();
        var registerResult = JsonSerializer.Deserialize<AuthResponse>(regContent, JsonOptions);
        var userId = registerResult!.User!.Id;
        string? lastAccessToken = null;

        // Act
        for (int i = 0; i < 3; i++)
        {
            var loginResponse = await HttpClient.PostAsJsonAsync("/api/v1/auth/login", new
            {
                Username = username,
                Password = TestPassword
            });
            Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
            var loginContent = await loginResponse.Content.ReadAsStringAsync();
            var loginResult = JsonSerializer.Deserialize<AuthResponse>(loginContent, JsonOptions);
            lastAccessToken = loginResult!.AccessToken;
        }

        // Get sessions count
        HttpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", lastAccessToken);
        var sessionsResponse = await HttpClient.GetAsync($"/api/v1/auth/users/{userId}/sessions");
        HttpClient.DefaultRequestHeaders.Authorization = null;

        Assert.Equal(HttpStatusCode.OK, sessionsResponse.StatusCode);

        var sessionsContent = await sessionsResponse.Content.ReadAsStringAsync();
        var sessionsResult = JsonSerializer.Deserialize<SessionsResponse>(sessionsContent, JsonOptions);

        Output.WriteLine($"Sessions count after 1 register + 3 logins: {sessionsResult?.Items?.Length ?? 0}");

        // Assert
        Assert.Equal(4, sessionsResult!.Items!.Length);
    }

    #endregion

    #region Role assignments

    /// <summary>
    /// User should have the USER role assigned after registration.
    /// </summary>
    [TimedFact]
    public async Task NewUser_HasUserRoleAssigned()
    {
        // Arrange
        var username = $"issue7_role_{Guid.NewGuid():N}";
        var email = $"{username}@example.com";

        var registerResponse = await HttpClient.PostAsJsonAsync("/api/v1/auth/register", new
        {
            Username = username,
            Email = email,
            Password = TestPassword,
            ConfirmPassword = TestPassword
        });
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        var regContent = await registerResponse.Content.ReadAsStringAsync();
        var registerResult = JsonSerializer.Deserialize<AuthResponse>(regContent, JsonOptions);
        var userId = registerResult!.User!.Id;
        var accessToken = registerResult.AccessToken;

        // Act
        HttpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        var userResponse = await HttpClient.GetAsync($"/api/v1/iam/users/{userId}");
        HttpClient.DefaultRequestHeaders.Authorization = null;

        // Assert
        var userContent = await userResponse.Content.ReadAsStringAsync();
        Output.WriteLine($"User response: {userContent}");
        Assert.Equal(HttpStatusCode.OK, userResponse.StatusCode);

        var userResult = JsonSerializer.Deserialize<JsonElement>(userContent, JsonOptions);

        Assert.True(userResult.TryGetProperty("roleIds", out var roleIds),
            "User should have 'roleIds' property");

        var roleIdsArray = roleIds.EnumerateArray().ToList();
        Output.WriteLine($"RoleIds count: {roleIdsArray.Count}");
        foreach (var roleId in roleIdsArray)
        {
            Output.WriteLine($"  RoleId: {roleId}");
        }

        Assert.True(roleIdsArray.Count >= 1, "User should have at least the USER role assigned");
    }

    /// <summary>
    /// Permissions should reflect the user's role assignments.
    /// </summary>
    [TimedFact]
    public async Task UserPermissions_ReflectRoleAssignments()
    {
        // Arrange
        var username = $"issue7_perm_{Guid.NewGuid():N}";
        var email = $"{username}@example.com";

        var registerResponse = await HttpClient.PostAsJsonAsync("/api/v1/auth/register", new
        {
            Username = username,
            Email = email,
            Password = TestPassword,
            ConfirmPassword = TestPassword
        });
        var regContent = await registerResponse.Content.ReadAsStringAsync();
        var registerResult = JsonSerializer.Deserialize<AuthResponse>(regContent, JsonOptions);
        var userId = registerResult!.User!.Id;
        var accessToken = registerResult.AccessToken;

        // Act
        HttpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        var permResponse = await HttpClient.GetAsync($"/api/v1/iam/users/{userId}/permissions");
        HttpClient.DefaultRequestHeaders.Authorization = null;

        // Assert
        var permContent = await permResponse.Content.ReadAsStringAsync();
        Output.WriteLine($"Permissions response: {permContent}");
        Assert.Equal(HttpStatusCode.OK, permResponse.StatusCode);

        var permResult = JsonSerializer.Deserialize<JsonElement>(permContent, JsonOptions);

        Assert.True(permResult.TryGetProperty("permissions", out var permissions),
            "Response should have 'permissions' property");

        var permissionsArray = permissions.EnumerateArray().ToList();
        Output.WriteLine($"Effective permissions count: {permissionsArray.Count}");
        foreach (var perm in permissionsArray)
        {
            Output.WriteLine($"  Permission: {perm}");
        }

        // User should have permissions from the USER role
        Assert.True(permissionsArray.Count > 0, 
            "User should have permissions from their assigned roles");
    }

    #endregion

    #region Static roles

    /// <summary>
    /// Static roles (ADMIN, USER) should be accessible even though they're not in DB.
    /// </summary>
    [TimedFact]
    public async Task GetUserRoles_IncludesStaticRoles()
    {
        // Arrange
        var username = $"issue8_static_{Guid.NewGuid():N}";
        var email = $"{username}@example.com";

        var registerResponse = await HttpClient.PostAsJsonAsync("/api/v1/auth/register", new
        {
            Username = username,
            Email = email,
            Password = TestPassword,
            ConfirmPassword = TestPassword
        });
        var regContent = await registerResponse.Content.ReadAsStringAsync();
        var registerResult = JsonSerializer.Deserialize<AuthResponse>(regContent, JsonOptions);
        var userId = registerResult!.User!.Id;
        var accessToken = registerResult.AccessToken;

        // Act
        HttpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        var userResponse = await HttpClient.GetAsync($"/api/v1/iam/users/{userId}");
        HttpClient.DefaultRequestHeaders.Authorization = null;

        // Assert
        var userContent = await userResponse.Content.ReadAsStringAsync();
        Output.WriteLine($"User response: {userContent}");
        Assert.Equal(HttpStatusCode.OK, userResponse.StatusCode);

        var userResult = JsonSerializer.Deserialize<JsonElement>(userContent, JsonOptions);
        
        // User should have static USER role
        if (userResult.TryGetProperty("roleIds", out var roleIds))
        {
            var roleIdsArray = roleIds.EnumerateArray().ToList();
            Output.WriteLine($"User roleIds count: {roleIdsArray.Count}");
            Assert.True(roleIdsArray.Count > 0, "User should have at least the USER role");
        }
    }

    #endregion

    #region Additional Verification Tests

    /// <summary>
    /// Verify that /me endpoint returns correct user info with permissions.
    /// </summary>
    [TimedFact]
    public async Task MeEndpoint_ReturnsUserWithPermissions()
    {
        // Arrange
        var username = $"me_test_{Guid.NewGuid():N}";
        var email = $"{username}@example.com";

        var registerResponse = await HttpClient.PostAsJsonAsync("/api/v1/auth/register", new
        {
            Username = username,
            Email = email,
            Password = TestPassword,
            ConfirmPassword = TestPassword
        });
        var regContent = await registerResponse.Content.ReadAsStringAsync();
        var registerResult = JsonSerializer.Deserialize<AuthResponse>(regContent, JsonOptions);
        var accessToken = registerResult!.AccessToken;

        // Act
        HttpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        var meResponse = await HttpClient.GetAsync("/api/v1/auth/me");
        HttpClient.DefaultRequestHeaders.Authorization = null;

        // Assert
        var meContent = await meResponse.Content.ReadAsStringAsync();
        Output.WriteLine($"/me response: {meContent}");
        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);

        var meResult = JsonSerializer.Deserialize<JsonElement>(meContent, JsonOptions);
        
        // Should have username
        Assert.True(meResult.TryGetProperty("username", out var usernameProperty));
        Assert.Equal(username, usernameProperty.GetString());

        // Should have permissions
        Assert.True(meResult.TryGetProperty("permissions", out var permissions));
        var permissionsArray = permissions.EnumerateArray().ToList();
        Assert.True(permissionsArray.Count > 0, "/me should return user's permissions");
    }

    /// <summary>
    /// Verify that change-password works for users who have a password.
    /// </summary>
    [TimedFact]
    public async Task ChangePassword_WhenUserHasPassword_Succeeds()
    {
        // Arrange
        var username = $"chgpass_{Guid.NewGuid():N}";
        var email = $"{username}@example.com";
        var newPassword = "NewSecure@Pass456!";

        var registerResponse = await HttpClient.PostAsJsonAsync("/api/v1/auth/register", new
        {
            Username = username,
            Email = email,
            Password = TestPassword,
            ConfirmPassword = TestPassword
        });
        var regContent = await registerResponse.Content.ReadAsStringAsync();
        var registerResult = JsonSerializer.Deserialize<AuthResponse>(regContent, JsonOptions);
        var userId = registerResult!.User!.Id;
        var accessToken = registerResult.AccessToken;

        // Act
        HttpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        var changeResponse = await HttpClient.PutAsJsonAsync($"/api/v1/auth/users/{userId}/identity/password", new
        {
            CurrentPassword = TestPassword,
            NewPassword = newPassword
        });
        HttpClient.DefaultRequestHeaders.Authorization = null;

        // Assert
        var changeContent = await changeResponse.Content.ReadAsStringAsync();
        Output.WriteLine($"Change password status: {changeResponse.StatusCode}, content: {changeContent}");
        Assert.Equal(HttpStatusCode.NoContent, changeResponse.StatusCode);

        // Verify can login with new password
        var loginResponse = await HttpClient.PostAsJsonAsync("/api/v1/auth/login", new
        {
            Username = username,
            Password = newPassword
        });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        // Verify cannot login with old password
        var oldLoginResponse = await HttpClient.PostAsJsonAsync("/api/v1/auth/login", new
        {
            Username = username,
            Password = TestPassword
        });
        Assert.Equal(HttpStatusCode.Unauthorized, oldLoginResponse.StatusCode);
    }

    /// <summary>
    /// Verify logout properly revokes the session.
    /// </summary>
    [TimedFact]
    public async Task Logout_RevokesSessionAndInvalidatesTokens()
    {
        // Arrange
        var username = $"logout_{Guid.NewGuid():N}";
        var email = $"{username}@example.com";

        var registerResponse = await HttpClient.PostAsJsonAsync("/api/v1/auth/register", new
        {
            Username = username,
            Email = email,
            Password = TestPassword,
            ConfirmPassword = TestPassword
        });
        var regContent = await registerResponse.Content.ReadAsStringAsync();
        var registerResult = JsonSerializer.Deserialize<AuthResponse>(regContent, JsonOptions);
        var accessToken = registerResult!.AccessToken;
        var refreshToken = registerResult.RefreshToken;

        // Act
        HttpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        var logoutResponse = await HttpClient.PostAsync("/api/v1/auth/logout", null);
        HttpClient.DefaultRequestHeaders.Authorization = null;

        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

        // Assert
        var refreshResponse = await HttpClient.PostAsJsonAsync("/api/v1/auth/refresh", new
        {
            RefreshToken = refreshToken
        });

        Output.WriteLine($"Refresh after logout status: {refreshResponse.StatusCode}");
        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
    }

    #endregion
}

// Response DTOs for deserializing API responses
public class AuthResponse
{
    public string AccessToken { get; set; } = "";
    public string RefreshToken { get; set; } = "";
    public string TokenType { get; set; } = "";
    public int ExpiresIn { get; set; }
    public AuthUserInfo? User { get; set; }
}

public class AuthUserInfo
{
    public Guid Id { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string[]? Roles { get; set; }
    public string[]? Permissions { get; set; }
    public bool IsAnonymous { get; set; }
}

public class SessionsResponse
{
    public SessionInfo[]? Items { get; set; }
}

public class SessionInfo
{
    public Guid Id { get; set; }
    public string? DeviceName { get; set; }
    public DateTime CreatedAt { get; set; }
}


